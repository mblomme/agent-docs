using System.Xml.Linq;

namespace Company.BoundedContext.DesignTests.Common;

public sealed class UseCaseContractRegressionTests : IDisposable
{
    private readonly string checkout = Path.Combine(
        Path.GetTempPath(),
        "hex-usecase-" + Guid.NewGuid().ToString("N")
    );

    [Theory]
    [InlineData(typeof(IInputOnly<>))]
    [InlineData(typeof(IWithOutput<,>))]
    [InlineData(typeof(IExtended<,>))]
    [InlineData(typeof(IInherited<,>))]
    public void ValidPortsAllowExtraParametersMembersAndInheritedExecute(
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        Assert.Empty(UseCaseContractInspection.Shape(type));
        Assert.Empty(UseCaseContractInspection.Execute(type));
        Assert.Empty(UseCaseContractInspection.AsyncExecute(type));
    }

    [Theory]
    [InlineData(typeof(NotInterface<>))]
    [InlineData(typeof(IHidden<>))]
    [InlineData(typeof(INoParameters))]
    [InlineData(typeof(IThreeParameters<,,>))]
    [InlineData(typeof(IInputOnly<string>))]
    public void InvalidShapesReportDeclarationRemedyWithoutCrashing(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        foreach (
            var inspect in new Func<Type, IEnumerable<string>>[]
            {
                UseCaseContractInspection.Shape,
                UseCaseContractInspection.Execute,
                UseCaseContractInspection.AsyncExecute,
            }
        )
        {
            var message = Assert.Single(inspect(type));
            Assert.Contains(
                "public generic interface",
                message,
                StringComparison.Ordinal
            );
            Assert.Contains(type.ToString(), message, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(typeof(IMissing<>))]
    [InlineData(typeof(IWrongInput<>))]
    [InlineData(typeof(IRefInput<>))]
    [InlineData(typeof(IOutInput<>))]
    [InlineData(typeof(IStaticExecute<>))]
    [InlineData(typeof(IGenericExecute<>))]
    [InlineData(typeof(IProtectedExecute<>))]
    public void InvalidExecuteSignaturesReportInputAndRemedy(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var message = Assert.Single(UseCaseContractInspection.Execute(type));
        Assert.Contains(type.ToString(), message, StringComparison.Ordinal);
        Assert.Contains(
            "add public instance Execute(TInput input)",
            message,
            StringComparison.Ordinal
        );
        Assert.Contains("by value", message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(typeof(ISynchronous<>))]
    [InlineData(typeof(IValueTaskPort<>))]
    [InlineData(typeof(IUnexpectedOutput<>))]
    [InlineData(typeof(IMissingOutput<,>))]
    [InlineData(typeof(IWrongOutput<,>))]
    [InlineData(typeof(IValueTaskOutput<,>))]
    [InlineData(typeof(IMethodOnlyAsync<>))]
    public void InvalidAsyncContractsReportExactTaskSignature(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var messages = UseCaseContractInspection.AsyncExecute(type).ToArray();
        Assert.Contains(
            messages,
            m =>
                m.Contains(
                    "Use the exact Task return type",
                    StringComparison.Ordinal
                )
        );
        Assert.All(
            messages,
            m => Assert.Contains(type.ToString(), m, StringComparison.Ordinal)
        );
    }

    [Fact]
    public void AsyncSuffixIsRejectedEvenAlongsideValidExecute()
    {
        var message = Assert.Single(
            UseCaseContractInspection.AsyncExecute(typeof(IAsyncSuffix<>))
        );
        Assert.Contains("rename", message, StringComparison.Ordinal);
        Assert.Contains(
            "ExecuteAsync to Execute",
            message,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData("Contracts/Driving/IQueryUseCase.cs", true)]
    [InlineData("Queries/IQueryUseCase.cs", false)]
    public void QueryPlacementUsesTheSameDeclarationChecks(
        string path,
        bool valid
    )
    {
        var violations = UseCaseContractInspection
            .DrivingDeclarations(
                [
                    (
                        path,
                        "namespace Example.Application.Contracts.Driving; public interface IQueryUseCase<TInput, TOutput> {}"
                    ),
                ],
                "Example.Application.Contracts.Driving",
                arity: 2,
                interfaceName: "IQueryUseCase"
            )
            .ToArray();
        if (valid)
            Assert.Empty(violations);
        else
            Assert.Contains(
                "move the IQueryUseCase`2",
                Assert.Single(violations),
                StringComparison.Ordinal
            );
    }

    [Fact]
    public void QueryOnlyDiscoveryAcceptsQueryWithoutInputOnlyPort() =>
        Assert.Empty(
            UseCaseContractInspection.Contracts(
                [typeof(UseCaseFixtures.IQueryUseCase<,>)],
                typeof(UseCaseFixtures.IQueryUseCase<,>).Namespace!,
                UseCaseContractInspection.Shape,
                arity: 2,
                interfaceName: "IQueryUseCase"
            )
        );

    [Fact]
    public void QueryOnlyDiscoveryReportsMissingQueryPort()
    {
        var message = Assert.Single(
            UseCaseContractInspection.Contracts(
                [
                    typeof(UseCaseFixtures.IUseCase<>),
                    typeof(UseCaseFixtures.IUseCase<,>),
                ],
                typeof(UseCaseFixtures.IUseCase<>).Namespace!,
                UseCaseContractInspection.Shape,
                arity: 2,
                interfaceName: "IQueryUseCase"
            )
        );
        Assert.Contains("IQueryUseCase`2", message, StringComparison.Ordinal);
        Assert.Contains("found 0", message, StringComparison.Ordinal);
        Assert.DoesNotContain("IUseCase`1", message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingPortFormsProduceExplicitDiscoveryFailures()
    {
        var none = UseCaseContractInspection
            .Contracts(
                [],
                "Example.Application.Contracts.Driving",
                UseCaseContractInspection.Shape
            )
            .ToArray();
        Assert.Equal(2, none.Length);
        Assert.Contains("IUseCase`1", none[0], StringComparison.Ordinal);
        Assert.Contains("IUseCase`2", none[1], StringComparison.Ordinal);
        Assert.All(
            none,
            m => Assert.Contains("found 0", m, StringComparison.Ordinal)
        );
        var one = Assert.Single(
            UseCaseContractInspection.Contracts(
                [typeof(UseCaseFixtures.IUseCase<>)],
                typeof(UseCaseFixtures.IUseCase<>).Namespace!,
                UseCaseContractInspection.Shape
            )
        );
        Assert.Contains("IUseCase`2", one, StringComparison.Ordinal);
        Assert.Empty(
            UseCaseContractInspection.Contracts(
                [
                    typeof(UseCaseFixtures.IUseCase<>),
                    typeof(UseCaseFixtures.IUseCase<,>),
                ],
                typeof(UseCaseFixtures.IUseCase<>).Namespace!,
                UseCaseContractInspection.Shape
            )
        );
    }

    [Theory]
    [InlineData(
        "Contracts/Driving/Ports.cs",
        "namespace Example.Application.Contracts.Driving;"
    )]
    [InlineData(
        "Contracts\\Driving\\Anything.cs",
        "namespace Example { namespace Application.Contracts.Driving {"
    )]
    public void ActualDeclarationsAcceptSharedFilesAndBothNamespaceStyles(
        string path,
        string prefix
    )
    {
        ArgumentNullException.ThrowIfNull(prefix);
        var suffix = prefix.EndsWith('{') ? "}}" : "";
        Assert.Empty(
            UseCaseContractInspection.DrivingDeclarations(
                [
                    (
                        path,
                        prefix
                            + " public interface IUseCase<T> {} public interface IUseCase<T,TResult> {}"
                            + suffix
                    ),
                ],
                "Example.Application.Contracts.Driving"
            )
        );
    }

    [Theory]
    [InlineData("Ports.cs")]
    [InlineData("Contracts/Driven/Ports.cs")]
    [InlineData("Contracts/Driving/Nested/Ports.cs")]
    public void CorrectNamespaceDoesNotExcuseWrongPhysicalPlacement(string path)
    {
        var messages = UseCaseContractInspection
            .DrivingDeclarations(
                [
                    (
                        path,
                        "namespace Example.Application.Contracts.Driving; public interface IUseCase<T> {} public interface IUseCase<T,TResult> {}"
                    ),
                ],
                "Example.Application.Contracts.Driving"
            )
            .ToArray();
        Assert.Equal(2, messages.Length);
        Assert.All(
            messages,
            m => Assert.Contains("move", m, StringComparison.Ordinal)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData(
        "namespace Wrong; public interface IUseCase<T> {} public interface IUseCase<T,TResult> {}"
    )]
    [InlineData(
        "namespace Example.Application.Contracts.Driving; public class IUseCase<T> {} public interface IUseCase<T,TResult,TOther> {}"
    )]
    [InlineData(
        "namespace Example.Application.Contracts.Driving; public class Container { public interface IUseCase<T> {} public interface IUseCase<T,TResult> {} }"
    )]
    public void EmptyFilesWrongNamespaceKindArityAndNestedDeclarationsCannotSatisfyPlacement(
        string text
    )
    {
        var messages = UseCaseContractInspection
            .DrivingDeclarations(
                [("Contracts/Driving/IUseCase.cs", text)],
                "Example.Application.Contracts.Driving"
            )
            .ToArray();
        Assert.Equal(2, messages.Length);
        Assert.All(
            messages,
            m =>
                Assert.Contains(
                    "missing source declaration",
                    m,
                    StringComparison.Ordinal
                )
        );
    }

    [Fact]
    public void ProjectDiscoveryFindsApplicationAndInspectsActualSources()
    {
        var output = CreateCheckout();
        var project = Path.Combine(checkout, "src", "App");
        var driving = Path.Combine(project, "Contracts", "Driving");
        Directory.CreateDirectory(driving);
        File.WriteAllText(
            Path.Combine(driving, "Ports.cs"),
            "namespace Example.Application.Contracts.Driving; public interface IUseCase<T> {} public interface IUseCase<T,TResult> {}"
        );
        Assert.Empty(Inspect(output));
        File.Move(
            Path.Combine(driving, "Ports.cs"),
            Path.Combine(project, "Ports.cs")
        );
        Assert.Equal(2, Inspect(output).Length);
    }

    [Fact]
    public void MissingCheckoutAndApplicationReferenceGiveRepairGuidance()
    {
        var message = Assert.Single(Inspect(checkout));
        Assert.Contains("source checkout", message, StringComparison.Ordinal);
        var output = CreateCheckout();
        File.Delete(Path.Combine(checkout, "src", "App", "App.csproj"));
        message = Assert.Single(Inspect(output));
        Assert.Contains("found 0", message, StringComparison.Ordinal);
        Assert.Contains(
            "Application ProjectReference",
            message,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain(checkout, message, StringComparison.Ordinal);
    }

    private static string[] Inspect(string output) =>
        UseCaseContractInspection
            .DrivingFolder(output, "Rules", "Example.Application")
            .ToArray();

    private string CreateCheckout()
    {
        var test = Path.Combine(checkout, "tests", "Rules");
        var app = Path.Combine(checkout, "src", "App");
        var output = Path.Combine(test, "bin", "Debug");
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(app);
        new XDocument(
            new XElement(
                "Project",
                new XElement(
                    "ItemGroup",
                    new XElement(
                        "ProjectReference",
                        new XAttribute("Include", "../../src/App/App.csproj")
                    )
                )
            )
        ).Save(Path.Combine(test, "Rules.csproj"));
        new XDocument(
            new XElement(
                "Project",
                new XElement(
                    "PropertyGroup",
                    new XElement("AssemblyName", "Example.Application")
                )
            )
        ).Save(Path.Combine(app, "App.csproj"));
        return output;
    }

    public void Dispose()
    {
        if (Directory.Exists(checkout))
            Directory.Delete(checkout, true);
    }

    public interface IInputOnly<T>
    {
        Task Execute(T input);
    }

    public interface IWithOutput<T, TResult>
    {
        Task<TResult> Execute(T input);
    }

    public interface IExtended<T, TResult>
    {
#pragma warning disable CA1068 // Fixture verifies extra parameters before the input.
        Task<TResult> Execute(CancellationToken token, T input);
#pragma warning restore CA1068
        void Other();
        void Execute();
    }

    public interface IInherited<T, TResult> : IWithOutput<T, TResult> { }

    public class NotInterface<T> { }

    private interface IHidden<T> { }

    public interface INoParameters { }

    public interface IThreeParameters<T, TResult, TOther> { }

    public interface IMissing<T> { }

    public interface IWrongInput<T>
    {
        Task Execute(string input);
    }

    public interface IRefInput<T>
    {
        Task Execute(ref T input);
    }

    public interface IOutInput<T>
    {
        Task Execute(out T input);
    }

    public interface IStaticExecute<T>
    {
#pragma warning disable CA1000 // Deliberately invalid port used to verify static method rejection.
        static Task Execute(T input) => Task.CompletedTask;
#pragma warning restore CA1000
    }

    public interface IGenericExecute<T>
    {
        Task Execute<TResult>(T input);
    }

    public interface IProtectedExecute<T>
    {
        protected Task Execute(T input);
    }

    public interface ISynchronous<T>
    {
        void Execute(T input);
    }

    public interface IValueTaskPort<T>
    {
        ValueTask Execute(T input);
    }

    public interface IUnexpectedOutput<T>
    {
        Task<int> Execute(T input);
    }

    public interface IMissingOutput<T, TResult>
    {
        Task Execute(T input);
    }

    public interface IWrongOutput<T, TResult>
    {
        Task<T> Execute(T input);
    }

    public interface IValueTaskOutput<T, TResult>
    {
        ValueTask<TResult> Execute(T input);
    }

    public interface IMethodOnlyAsync<T>
    {
        Task ExecuteAsync(T input);
    }

    public interface IAsyncSuffix<T> : IInputOnly<T>
    {
        Task ExecuteAsync(T input);
    }
}
