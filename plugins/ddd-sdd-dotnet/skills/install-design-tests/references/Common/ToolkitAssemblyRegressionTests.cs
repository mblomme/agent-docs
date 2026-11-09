using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Company.BoundedContext.DesignTests.Common;

public class ToolkitAssemblyRegressionTests
{
    private const string DomainSource = """
        namespace DddToolkit.Domain {
            public interface IDomainEvent {}
            public interface IAggregateRoot {}
            public interface IRepository {}
            public abstract class Entity<T> {}
            public abstract class AggregateRoot<T> : Entity<T>, IAggregateRoot {}
            public interface IRepository<T, TId> : IRepository where T : AggregateRoot<TId> {}
        }
        """;

    private const string ApplicationSource = """
        namespace DddToolkit.Application.Contracts.Driving {
            public interface IUseCase<T> { System.Threading.Tasks.Task Execute(T input); }
            public interface IUseCase<T, R> { System.Threading.Tasks.Task<R> Execute(T input); }
            public interface IQueryUseCase<T, R> { System.Threading.Tasks.Task<R> Execute(T input); }
            public interface IPolicy<in T> where T : DddToolkit.Domain.IDomainEvent { System.Threading.Tasks.Task Execute(T input); }
        }
        namespace DddToolkit.Application.Contracts.Driven {
            public interface IQuery<T> { System.Threading.Tasks.Task<T> Fetch(); }
            public interface IQuery<T, R> { System.Threading.Tasks.Task<R> Fetch(T input); }
            public interface IUnitOfWork {
                System.Threading.Tasks.Task Do();
                System.Threading.Tasks.Task Save<T>(DddToolkit.Domain.IAggregateRoot aggregate) where T : DddToolkit.Domain.IRepository;
                T Repo<T>() where T : DddToolkit.Domain.IRepository;
            }
        }
        """;

    [Fact]
    public void ProductAndToolkitAssembliesAreDiscoveredSeparately()
    {
        var toolkitDomain = Assembly.Load(
            Emit(Compile("DddToolkit.Domain", DomainSource))
        );
        var toolkitApplication = Assembly.Load(
            Emit(Compile("DddToolkit.Application", ""))
        );
        var domain = Assembly.Load(Emit(Compile("Example.Product.Domain", "")));
        var application = Assembly.Load(
            Emit(Compile("Example.Product.Application", ""))
        );
        Assembly[] assemblies =
        [
            toolkitDomain,
            toolkitApplication,
            domain,
            application,
        ];
        Assert.Same(domain, AssemblyDiscovery.DomainAssembly(assemblies));
        Assert.Same(
            application,
            AssemblyDiscovery.ApplicationAssembly(assemblies)
        );
        Assert.Same(
            toolkitDomain,
            AssemblyDiscovery.ToolkitAssembly(assemblies, "Domain")
        );
        Assert.Same(
            toolkitApplication,
            AssemblyDiscovery.ToolkitAssembly(assemblies, "Application")
        );
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            AssemblyDiscovery.ToolkitAssembly([domain], "Domain")
        );
        Assert.Contains(
            "lib/DddToolkit.Domain",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ProductRepositoriesAndAggregatesUseToolkitContractsAcrossAssemblies()
    {
        var domainBytes = Emit(Compile("DddToolkit.Domain", DomainSource));
        var productBytes = Emit(
            Compile(
                "Example.Product.Domain",
                """
                namespace Example.Product.Domain.Orders {
                    public class Order : DddToolkit.Domain.AggregateRoot<int> {}
                    public interface IOrders : DddToolkit.Domain.IRepository<Order, int> {}
                }
                """,
                MetadataReference.CreateFromImage(domainBytes)
            )
        );
        using var context = new FixtureAssemblies(domainBytes, productBytes);
        var toolkit = context.LoadFromAssemblyName(
            new AssemblyName("DddToolkit.Domain")
        );
        var product = context.LoadFromAssemblyName(
            new AssemblyName("Example.Product.Domain")
        );
        var aggregate = AggregateContractInspection.SharedType(
            toolkit,
            "AggregateRoot`1"
        );
        var repository = AggregateContractInspection.SharedType(
            toolkit,
            "IRepository`2"
        );
        Assert.Single(
            DomainDiscovery.AggregateRoots(product.GetTypes(), aggregate)
        );
        Assert.Empty(
            RepositoryInspection.AggregateRepositories(
                product.GetTypes(),
                aggregate,
                repository
            )
        );
        Assert.Empty(
            RepositoryInspection.RepositoryInterfaces(
                product,
                [product, toolkit],
                RepositoryInspection.IsRepositoryInterface
            )
        );
        var applicationBytes = Emit(
            Compile(
                "DddToolkit.Application",
                ApplicationSource,
                MetadataReference.CreateFromImage(domainBytes)
            )
        );
        // Use compilation symbols to verify the cross-assembly policy constraint without CLR load-context coupling.
        var compilation = Compile(
            "Example.Product.Application",
            "",
            MetadataReference.CreateFromImage(domainBytes),
            MetadataReference.CreateFromImage(applicationBytes)
        );
        var policy = compilation.GetTypeByMetadataName(
            "DddToolkit.Application.Contracts.Driving.IPolicy`1"
        )!;
        Assert.Equal(
            "DddToolkit.Domain.IDomainEvent",
            Assert
                .Single(policy.TypeParameters[0].ConstraintTypes)
                .ToDisplayString()
        );
    }

    [Fact]
    public void ToolkitPortShapesAndConstraintsRemainEnforcedAcrossAssemblies()
    {
        var domainBytes = Emit(Compile("DddToolkit.Domain", DomainSource));
        var applicationBytes = Emit(
            Compile(
                "DddToolkit.Application",
                ApplicationSource,
                MetadataReference.CreateFromImage(domainBytes)
            )
        );
        using var context = new FixtureAssemblies(
            domainBytes,
            applicationBytes
        );
        var domain = context.LoadFromAssemblyName(
            new AssemblyName("DddToolkit.Domain")
        );
        var application = context.LoadFromAssemblyName(
            new AssemblyName("DddToolkit.Application")
        );
        var policy = application.GetType(
            "DddToolkit.Application.Contracts.Driving.IPolicy`1"
        )!;
        var work = application.GetType(
            "DddToolkit.Application.Contracts.Driven.IUnitOfWork"
        )!;
        Assert.Empty(PolicyContractInspection.Shape(policy));
        Assert.Empty(
            PolicyContractInspection.EventParameter(
                policy,
                domain.GetType("DddToolkit.Domain.IDomainEvent")!
            )
        );
        Assert.Empty(UnitOfWorkContractInspection.Shape(work));
        Assert.Empty(
            UnitOfWorkContractInspection.Save(
                work,
                domain.GetType("DddToolkit.Domain.IAggregateRoot")!,
                domain.GetType("DddToolkit.Domain.IRepository")!
            )
        );
        Assert.Empty(
            UnitOfWorkContractInspection.Repo(
                work,
                domain.GetType("DddToolkit.Domain.IRepository")!
            )
        );
        foreach (var arity in new[] { 1, 2 })
        {
            var port = application.GetType(
                "DddToolkit.Application.Contracts.Driving.IUseCase`" + arity
            )!;
            Assert.Empty(UseCaseContractInspection.Shape(port));
            Assert.Empty(UseCaseContractInspection.Execute(port));
            Assert.Empty(UseCaseContractInspection.AsyncExecute(port));
        }
    }

    [Fact]
    public void UnavailableAssembliesHaveRebuildGuidance()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            AssemblyDiscovery.Load("Unavailable.Toolkit.Fixture")
        );
        Assert.Contains(
            "Cannot load Unavailable.Toolkit.Fixture",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "rebuild the solution",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WorkflowsAndNamedQueriesResolveExternalToolkitPorts(
        bool forbiddenQueryDependency
    )
    {
        var domainBytes = Emit(Compile("DddToolkit.Domain", DomainSource));
        var domainReference = MetadataReference.CreateFromImage(domainBytes);
        var applicationReference = MetadataReference.CreateFromImage(
            Emit(
                Compile(
                    "DddToolkit.Application",
                    ApplicationSource,
                    domainReference
                )
            )
        );
        var compilation = Compile(
                "Example.Product.Application",
                "",
                domainReference,
                applicationReference
            )
            .AddSyntaxTrees(
                CSharpSyntaxTree.ParseText(
                    """
                    namespace Example.Product.Application.Contracts.Driven;
                    public interface ICount : DddToolkit.Application.Contracts.Driven.IQuery<int> {}
                    """,
                    path: "/application/Contracts/Driven/ICount.cs"
                ),
                CSharpSyntaxTree.ParseText(
                    """
                    namespace Example.Product.Application;
                    public record Input;
                    public class Capture(DddToolkit.Application.Contracts.Driven.IUnitOfWork work) : DddToolkit.Application.Contracts.Driving.IUseCase<Input> {
                        public async System.Threading.Tasks.Task Execute(Input input) { await work.Do(); }
                    }
                    """,
                    path: "/application/Capture.cs"
                ),
                CSharpSyntaxTree.ParseText(
                    $$"""
                    namespace Example.Product.Application.Queries;
                    public record Input;
                    public class Count(Contracts.Driven.ICount count{{(
                        forbiddenQueryDependency
                            ? ", DddToolkit.Application.Contracts.Driven.IUnitOfWork work, DddToolkit.Domain.IAggregateRoot root"
                            : ""
                    )}}) : DddToolkit.Application.Contracts.Driving.IQueryUseCase<Input, int> {
                        public System.Threading.Tasks.Task<int> Execute(Input input) => count.Fetch();
                    }
                    """,
                    path: "/application/Queries/Count.cs"
                )
            );
        var findings = ApplicationWorkflowInspection.Inspect(
            compilation,
            "Example.Product.Application",
            "/application",
            "DddToolkit.Application"
        );
        if (forbiddenQueryDependency)
        {
            Assert.Contains(findings, v => v.Rule == "query-unit-of-work");
            Assert.Contains(findings, v => v.Rule == "query-domain");
        }
        else
            Assert.Empty(findings);
        Assert.Empty(
            QueryPortInspection.Inspect(
                compilation,
                "Example.Product.Application",
                "/application",
                "DddToolkit.Application"
            )
        );
        Assert.Empty(
            UseCaseImplementationInspection.Inspect(
                compilation,
                "DddToolkit.Application.Contracts.Driving",
                "DddToolkit.Domain.IRepository",
                "DddToolkit.Application.Contracts.Driven.IUnitOfWork",
                "DddToolkit.Domain.IAggregateRoot"
            )
        );
    }

    private sealed class FixtureAssemblies(params byte[][] images)
        : AssemblyLoadContext(isCollectible: true),
            IDisposable
    {
        protected override Assembly? Load(AssemblyName name)
        {
            foreach (var bytes in images)
            {
                using var stream = new MemoryStream(bytes);
                using var module = Mono.Cecil.ModuleDefinition.ReadModule(
                    stream
                );
                if (module.Assembly.Name.Name == name.Name)
                    return LoadFromStream(new MemoryStream(bytes));
            }
            return null;
        }

        public void Dispose() => Unload();
    }

    private static CSharpCompilation Compile(
        string assemblyName,
        string source,
        params MetadataReference[] additional
    ) =>
        CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Where(path =>
                    Path.GetFileNameWithoutExtension(path)
                        is not ("DddToolkit.Domain" or "DddToolkit.Application")
                )
                .Select(path => MetadataReference.CreateFromFile(path))
                .Concat(additional),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

    private static byte[] Emit(CSharpCompilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(
            result.Success,
            string.Join(Environment.NewLine, result.Diagnostics)
        );
        return stream.ToArray();
    }
}
