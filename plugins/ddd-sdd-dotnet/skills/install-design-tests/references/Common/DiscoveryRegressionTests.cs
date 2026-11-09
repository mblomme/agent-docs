using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Company.BoundedContext.DesignTests.Common;

public class DiscoveryRegressionTests
{
    private const string Shared = """
        namespace DddToolkit.Domain
        {
            public abstract class Entity<T> { }
            public abstract class AggregateRoot<T> : Entity<T> { }
            public abstract class ValueObject { }
        }
        """;

    [Fact]
    public void EmptyDomainAssemblyIsDiscoveredByMetadataName()
    {
        var domain = Load("", "Different.Company.Domain");
        Assert.Same(domain, AssemblyDiscovery.DomainAssembly([domain]));
        var error = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            AggregateContractInspection.SharedType(domain, "Entity`1")
        );
        Assert.Contains("found 0", error.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Declare Entity",
            error.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void MissingAndAmbiguousDomainAssembliesGiveDiscoveryGuidance()
    {
        Assert.Throws<Xunit.Sdk.TrueException>(() =>
            AssemblyDiscovery.DomainAssembly([])
        );
        var error = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            AssemblyDiscovery.DomainAssembly([
                Load("", "First.Domain"),
                Load("", "Second.Domain"),
            ])
        );
        Assert.Contains("found 2", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DiscoveryUsesTypeIdentityAndUniqueAggregateNamespace()
    {
        var domain = Load(
            Shared
                + """
                namespace Example.Sales.Domain.Orders
                {
                    public class Order : global::DddToolkit.Domain.AggregateRoot<int> { }
                    public class Line : global::DddToolkit.Domain.Entity<int> { }
                    public class GenericLine<T> : global::DddToolkit.Domain.Entity<T> { }
                    public abstract class AbstractLine : global::DddToolkit.Domain.Entity<int> { }
                }
                namespace Example.Sales.Domain.Orders.Children { public class Detail : global::DddToolkit.Domain.Entity<int> { } }
                namespace Example.Sales.Domain.Invoices { public class Invoice : global::DddToolkit.Domain.AggregateRoot<int> { } }
                namespace Unrelated { public abstract class Entity<T> { } public class Impostor : Entity<int> { } }
                """,
            "Example.Sales.Domain"
        );
        var entities = Entities(domain);
        Assert.Equal(5, entities.Length);
        var aggregates = DomainDiscovery.Aggregates(
            entities,
            AggregateContractInspection.SharedType(domain, "AggregateRoot`1")
        );
        Assert.Equal(2, aggregates.Length);
        Assert.Equal(
            3,
            aggregates.Single(a => a.Root.Name == "Order").Members.Length
        );
        Assert.Empty(aggregates.Single(a => a.Root.Name == "Invoice").Members);
    }

    [Fact]
    public void AggregateDiscoveryUsesConcreteBaseClassInheritanceRegardlessOfAssemblyName()
    {
        var domain = Load(
            """
            namespace DddToolkit.Domain
            {
                public interface IAggregateRoot { }
                public abstract class AggregateRoot<T> { }
            }
            namespace Other.Product.Domain.Orders
            {
                public abstract class AbstractOrder<T> : global::DddToolkit.Domain.AggregateRoot<T> { }
                public class Order : AbstractOrder<int> { }
                public class DirectRoot : global::DddToolkit.Domain.AggregateRoot<int> { }
                public class MarkerOnly : global::DddToolkit.Domain.IAggregateRoot { }
                public class Unrelated { }
            }
            namespace Impostors
            {
                public abstract class AggregateRoot<T> { }
                public class WrongRoot : AggregateRoot<int> { }
            }
            """,
            "Unrelated.Assembly.Domain"
        );
        var roots = DomainDiscovery.AggregateRoots(
            domain.GetTypes(),
            AggregateContractInspection.SharedType(domain, "AggregateRoot`1")
        );
        Assert.Equal(
            ["DirectRoot", "Order"],
            roots.Select(type => type.Name).Order().ToArray()
        );
    }

    [Theory]
    [InlineData(
        "namespace Example.Sales.Domain.Strays { public class Child : global::DddToolkit.Domain.Entity<int> { } }",
        "found 0"
    )]
    [InlineData(
        "namespace Example.Sales.Domain.Orders { public class Second : global::DddToolkit.Domain.AggregateRoot<int> { } }",
        "ambiguous"
    )]
    [InlineData(
        "namespace Example.Sales.Domain.Orders.Nested { public class Second : global::DddToolkit.Domain.AggregateRoot<int> { } public class Child : global::DddToolkit.Domain.Entity<int> { } }",
        "found 2"
    )]
    public void OrphanedAndAmbiguousOwnershipFailClearly(
        string extra,
        string expected
    )
    {
        var domain = Load(
            Shared
                + "namespace Example.Sales.Domain.Orders { public class Order : global::DddToolkit.Domain.AggregateRoot<int> { } }"
                + extra,
            "Example.Sales.Domain"
        );
        var error = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DomainDiscovery.Aggregates(
                Entities(domain),
                AggregateContractInspection.SharedType(
                    domain,
                    "AggregateRoot`1"
                )
            )
        );
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TypesInOtherNamespacesDoNotShadowToolkitContracts()
    {
        var domain = Load(
            Shared
                + "namespace Other.Context.Domain.Shared { public abstract class Entity<T> { } }",
            "Example.Sales.Domain"
        );
        Assert.Equal(
            "DddToolkit.Domain.Entity`1",
            AggregateContractInspection.SharedType(domain, "Entity`1").FullName
        );
    }

    [Theory]
    [InlineData(
        "public class Sample { public static Sample Capture() => new(); }",
        true
    )]
    [InlineData(
        "public class Sample { public static Sample Reconstitute() => new(); }",
        false
    )]
    [InlineData(
        "public class Sample { public Sample Capture() => new(); }",
        false
    )]
    [InlineData(
        "public class Sample { public static Sample Capture<T>() => new(); }",
        false
    )]
    public void StructuralFactoryDiscoveryDoesNotNeedBusinessArguments(
        string source,
        bool valid
    )
    {
        var type = Load(source, "FactoryExamples").GetType("Sample")!;
        Assert.Equal(
            valid,
            !AggregateCreationInspection.StructuralFactory(type).Any()
        );
    }

    [Fact]
    public void DependencyGuardAcceptsDiscoveryAndRejectsProductionTypeReferences()
    {
        var production = Compile(
            "public class Aggregate { public static int Value => 1; }",
            "External.Domain"
        );
        var reference = MetadataReference.CreateFromImage(production);
        foreach (
            var source in new[]
            {
                "public class Test : Aggregate { }",
                "public class Test { public object Inspect() => typeof(Aggregate); }",
                "public class Test { public int Inspect() => Aggregate.Value; }",
            }
        )
        {
            using var input = new MemoryStream(
                Compile(source, "Consumer.Tests", reference)
            );
            Assert.Contains(
                ProductionDependencyInspection.References(
                    input,
                    ["External.Domain"]
                ),
                m => m.Contains("Aggregate", StringComparison.Ordinal)
            );
        }
        using var independent = new MemoryStream(
            Compile(
                "public class Test { public System.Type Inspect(System.Reflection.Assembly assembly) => assembly.GetType(\"Aggregate\"); }",
                "Independent.Tests",
                reference
            )
        );
        Assert.Empty(
            ProductionDependencyInspection.References(
                independent,
                ["External.Domain"]
            )
        );
    }

    [Fact]
    public void InspectorsRejectUnrelatedContractsWithIdenticalNames()
    {
        var domain = Load(
            """
            namespace Expected { public interface IDomainEvent { } public interface IAggregateRoot { } public interface IRepository { } }
            namespace Wrong { public interface IDomainEvent { } public interface IAggregateRoot { } public interface IRepository { } }
            public interface Policy<in T> where T : Wrong.IDomainEvent { }
            public interface Work {
                System.Threading.Tasks.Task Save<T>(Wrong.IAggregateRoot aggregate) where T : Wrong.IRepository;
                T Repo<T>() where T : Wrong.IRepository;
            }
            """,
            "Contracts"
        );
        Assert.NotEmpty(
            PolicyContractInspection.EventParameter(
                domain.GetType("Policy`1")!,
                domain.GetType("Expected.IDomainEvent")!
            )
        );
        Assert.NotEmpty(
            UnitOfWorkContractInspection.Save(
                domain.GetType("Work")!,
                domain.GetType("Expected.IAggregateRoot")!,
                domain.GetType("Expected.IRepository")!
            )
        );
        Assert.NotEmpty(
            UnitOfWorkContractInspection.Repo(
                domain.GetType("Work")!,
                domain.GetType("Expected.IRepository")!
            )
        );
    }

    [Fact]
    public void ApplicationInspectionUsesDiscoveredDomainNames()
    {
        var bytes = Compile(
            "namespace DddToolkit.Domain { public interface IRepository { } public interface IAggregateRoot { } }",
            "Other.Tenant.Domain"
        );
        var domain = Assembly.Load(bytes);
        var source = """
            namespace DddToolkit.Application.Contracts.Driving { public interface IUseCase<T> { System.Threading.Tasks.Task Execute(T input); } }
            namespace DddToolkit.Application.Contracts.Driven { public interface IUnitOfWork { } }
            namespace Another.Product.Application {
                public interface IOrders : DddToolkit.Domain.IRepository { }
                public class Capture(IOrders repository) : DddToolkit.Application.Contracts.Driving.IUseCase<bool> {
                    public System.Threading.Tasks.Task Execute(bool input) => System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """;
        var compilation = CSharpCompilation.Create(
            "Another.Product.Application",
            [CSharpSyntaxTree.ParseText(source)],
            PlatformReferences()
                .Append(MetadataReference.CreateFromImage(bytes)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        Assert.Contains(
            UseCaseImplementationInspection.InspectApplication(
                compilation,
                "Another.Product.Application",
                domain
            ),
            v => v.Rule == "injection"
        );
    }

    private static Type[] Entities(Assembly domain) =>
        DomainDiscovery.Entities(
            domain.GetTypes(),
            AggregateContractInspection.SharedType(domain, "Entity`1")
        );

    private static Assembly Load(string source, string name) =>
        Assembly.Load(Compile(source, name));

    private static IEnumerable<MetadataReference> PlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path =>
                Path.GetFileNameWithoutExtension(path) != "DddToolkit.Domain"
            )
            .Select(path => MetadataReference.CreateFromFile(path));

    private static byte[] Compile(
        string source,
        string name,
        params MetadataReference[] additional
    )
    {
        var references = PlatformReferences().Concat(additional);
        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        using var output = new MemoryStream();
        var result = compilation.Emit(output);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        return output.ToArray();
    }
}
