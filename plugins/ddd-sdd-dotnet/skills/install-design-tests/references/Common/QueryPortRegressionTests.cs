using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Company.BoundedContext.DesignTests.Common;

public class QueryPortRegressionTests
{
    private const string Common = """
        namespace Example.Application.Contracts.Driven;
        public interface IQuery<TOutput> { System.Threading.Tasks.Task<TOutput> Fetch(); }
        public interface IQuery<TInput, TOutput> { System.Threading.Tasks.Task<TOutput> Fetch(TInput input); }
        """;
    private const string Driving = """
        namespace Example.Application.Contracts.Driving;
        public interface IUseCase<T> {}
        public interface IUseCase<T, R> {}
        public interface IQueryUseCase<T, R> {}
        """;

    [Fact]
    public void NoNamedPortsOrWorkflowsAreRequired() => Assert.Empty(Inspect());

    [Theory]
    [InlineData("class Input {}")]
    [InlineData("record Input(int Count);")]
    [InlineData("struct Input {}")]
    [InlineData("record struct Input(int Count);")]
    public void ContractsDataTypesAndMultipleInterfaceDependenciesPass(
        string declaration
    ) =>
        Assert.Empty(
            Inspect(
                (
                    "Contracts/Data/Input.cs",
                    "namespace Example.Application.Contracts.Data; public "
                        + declaration
                ),
                (
                    "Contracts/Driven/Named.cs",
                    """
                    namespace Example.Application.Contracts.Driven;
                    public interface IFind : IQuery<Example.Application.Contracts.Data.Input, int[]> {}
                    public interface ICount : IQuery<int> {}
                    public interface IInherited : IFind {}
                    """
                ),
                (
                    "Run.cs",
                    """
                    using Example.Application.Contracts.Driven;
                    namespace Example.Application;
                    public class Run(IFind find, IQuery<int> count, IQuery<int, bool> exists) : Contracts.Driving.IUseCase<int> {}
                    public class Read(ICount count, IInherited find) : Contracts.Driving.IQueryUseCase<int, int> {}
                    """
                )
            )
        );

    [Theory]
    [InlineData("int")]
    [InlineData("bool")]
    [InlineData("float")]
    [InlineData("string")]
    [InlineData("System.Guid")]
    [InlineData("System.DateOnly?")]
    [InlineData("System.DayOfWeek")]
    public void ScalarInputsAndResultCollectionsPass(string scalar) =>
        Assert.Empty(
            Inspect(
                (
                    "Contracts/Driven/Named.cs",
                    $"namespace Example.Application.Contracts.Driven; public interface IFind : IQuery<{scalar}, System.Collections.Generic.IReadOnlyList<{scalar}>> {{}}"
                )
            )
        );

    [Theory]
    [InlineData("Elsewhere/Named.cs", "Example.Application.Contracts.Driven")]
    [InlineData(
        "Contracts/Driven/Nested/Named.cs",
        "Example.Application.Contracts.Driven"
    )]
    [InlineData("Contracts/Driven/Named.cs", "Other")]
    public void NamedPortPlacementIsChecked(string path, string ns) =>
        Finding(
            Inspect(
                (
                    path,
                    $"namespace {ns}; public interface IFind : Example.Application.Contracts.Driven.IQuery<int> {{}}"
                )
            ),
            "placement",
            "IFind"
        );

    [Fact]
    public void CommonPortPlacementIsChecked() =>
        Finding(Inspect(commonPath: "Wrong/IQuery.cs"), "placement", "IQuery");

    [Theory]
    [InlineData("int Fetch();")]
    [InlineData("System.Threading.Tasks.Task<TOutput> FetchAsync();")]
    [InlineData(
        "System.Threading.Tasks.Task<TOutput> Fetch(int first, int second);"
    )]
    [InlineData("System.Threading.Tasks.Task<TOutput> Fetch<T>();")]
    [InlineData(
        "System.Threading.Tasks.Task<TOutput> Fetch(); int Count { get; }"
    )]
    public void InvalidCommonFetchFails(string member) =>
        Finding(
            Inspect(
                common: Common.Replace(
                    "System.Threading.Tasks.Task<TOutput> Fetch();",
                    member,
                    StringComparison.Ordinal
                )
            ),
            "fetch",
            "Fetch"
        );

    [Theory]
    [InlineData("internal interface IQuery<TOutput>")]
    [InlineData("public interface IQuery<out TOutput>")]
    public void InvalidCommonShapeFails(string declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        // Variance requires a covariant return to make the fixture compilable; the shape remains invalid.
        var common = Common.Replace(
            "public interface IQuery<TOutput>",
            declaration,
            StringComparison.Ordinal
        );
        if (declaration.Contains("out", StringComparison.Ordinal))
            common = common.Replace(
                "System.Threading.Tasks.Task<TOutput> Fetch();",
                "TOutput Fetch();",
                StringComparison.Ordinal
            );
        Finding(Inspect(common: common), "shape", "IQuery");
    }

    [Theory]
    [InlineData("IQuery<int>", "int Extra();")]
    [InlineData("IQuery<int>", "int Count { get; }")]
    [InlineData(
        "IQuery<int>",
        "System.Threading.Tasks.Task<int> Fetch(int other);"
    )]
    [InlineData("IQuery<int>, IQuery<int, int>", "")]
    public void NamedPortsMustRepresentOnlyOneFetch(
        string bases,
        string members
    ) =>
        Finding(
            Inspect(
                (
                    "Contracts/Driven/Named.cs",
                    $"namespace Example.Application.Contracts.Driven; public interface IFind : {bases} {{ {members} }}"
                )
            ),
            "single-query",
            "IFind"
        );

    [Fact]
    public void InheritedExtraMembersFail() =>
        Finding(
            Inspect(
                (
                    "Contracts/Driven/Named.cs",
                    """
                    namespace Example.Application.Contracts.Driven;
                    public interface IExtra { int Count { get; } }
                    public interface IFind : IQuery<int>, IExtra {}
                    """
                )
            ),
            "single-query",
            "IFind"
        );

    [Theory]
    [InlineData("input")]
    [InlineData("output")]
    public void MisplacedDataObjectsFail(string role) =>
        Finding(
            Inspect(
                (
                    "Other/Data.cs",
                    "namespace Example.Application.Contracts.Data; public record Data(int Value);"
                ),
                (
                    "Contracts/Driven/Named.cs",
                    "namespace Example.Application.Contracts.Driven; public interface IFind : "
                        + (
                            role == "input"
                                ? "IQuery<Example.Application.Contracts.Data.Data, int>"
                                : "IQuery<System.Collections.Generic.List<Example.Application.Contracts.Data.Data>>"
                        )
                        + " {}"
                )
            ),
            role,
            "Contracts/Data"
        );

    [Fact]
    public void InputCollectionsMustBeBundled() =>
        Finding(
            Inspect(
                (
                    "Contracts/Driven/Named.cs",
                    "namespace Example.Application.Contracts.Driven; public interface IFind : IQuery<int[], int> {}"
                )
            ),
            "input",
            "Contracts/Data"
        );

    [Theory]
    [InlineData("IUseCase<int>")]
    [InlineData("IUseCase<int, int>")]
    [InlineData("IQueryUseCase<int, int>")]
    public void ConcreteDependenciesInInheritedConstructorsFail(
        string workflow
    ) =>
        Finding(
            Inspect(
                (
                    "Run.cs",
                    $$"""
                    using Example.Application.Contracts.Driven;
                    namespace Example.Application;
                    public class Reader : IQuery<int> { public System.Threading.Tasks.Task<int> Fetch() => null!; }
                    public abstract class Base<T> { protected Base() {} protected Base(T reader) {} }
                    public class Run : Base<Reader>, Contracts.Driving.{{workflow}} {}
                    """
                )
            ),
            "injection",
            "concrete query implementation"
        );

    [Fact]
    public void BareGenericDependenciesAlsoValidateDtos() =>
        Finding(
            Inspect(
                (
                    "Run.cs",
                    """
                    namespace Example.Application;
                    public record Input;
                    public class Run(Contracts.Driven.IQuery<Input, int> query) : Contracts.Driving.IUseCase<int> {}
                    """
                )
            ),
            "input",
            "Input"
        );

    [Fact]
    public void UnrelatedQueryNamesAreIgnored() =>
        Assert.Empty(
            Inspect(
                (
                    "Other.cs",
                    "namespace Other; public interface IQuery<T> { int Extra(); } public interface IFind : IQuery<int> {}"
                )
            )
        );

    [Fact]
    public void CompilationFailuresAreReported() =>
        Finding(
            Inspect(("Broken.cs", "class Broken : Missing {}")),
            "analysis",
            "Repair"
        );

    [Fact]
    public void MissingContractsAreReported() =>
        Finding(Inspect(common: ""), "shape", "Missing");

    [Fact]
    public void MissingSourceCheckoutIsReported() =>
        Finding(
            QueryPortInspection.Checkout("Nonexistent.Application"),
            "analysis",
            "Application"
        );

    [Theory]
    [InlineData("class Result {}")]
    [InlineData("record Result(int Count);")]
    [InlineData("struct Result {}")]
    [InlineData("record struct Result(int Count);")]
    public void ResultDtosAndNestedCollectionsPass(string declaration) =>
        Assert.Empty(
            Inspect(
                (
                    "Contracts/Data/Result.cs",
                    "namespace Example.Application.Contracts.Data; public "
                        + declaration
                ),
                (
                    "Contracts/Driven/Named.cs",
                    "namespace Example.Application.Contracts.Driven; public interface IFind : IQuery<System.Collections.Generic.IReadOnlyList<Example.Application.Contracts.Data.Result[]>> {}"
                )
            )
        );

    [Fact]
    public void CorrectDtoFolderWithWrongNamespaceFails() =>
        Finding(
            Inspect(
                (
                    "Contracts/Data/Input.cs",
                    "namespace Other; public record Input;"
                ),
                (
                    "Contracts/Driven/Named.cs",
                    "namespace Example.Application.Contracts.Driven; public interface IFind : IQuery<Other.Input, int> {}"
                )
            ),
            "input",
            "Contracts/Data"
        );

    [Theory]
    [InlineData("ref TInput input")]
    [InlineData("TInput input = default!")]
    [InlineData("TOutput input")]
    public void IncorrectFetchParameterFails(string parameter) =>
        Finding(
            Inspect(
                common: Common.Replace(
                    "Fetch(TInput input)",
                    "Fetch(" + parameter + ")",
                    StringComparison.Ordinal
                )
            ),
            "fetch",
            "Fetch"
        );

    private static void Finding(
        IReadOnlyList<QueryPortInspection.Violation> findings,
        string rule,
        string fragment
    )
    {
        if (rule != "analysis")
            Assert.DoesNotContain(findings, v => v.Rule == "analysis");
        Assert.Contains(
            findings,
            v =>
                v.Rule == rule
                && v.Message.Contains(fragment, StringComparison.Ordinal)
        );
    }

    private static readonly MetadataReference[] References = (
        (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!
    )
        .Split(Path.PathSeparator)
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray();

    private static IReadOnlyList<QueryPortInspection.Violation> Inspect(
        (string Path, string Text) first = default,
        (string Path, string Text) second = default,
        (string Path, string Text) third = default,
        string common = Common,
        string commonPath = "Contracts/Driven/IQuery.cs"
    )
    {
        var root = Path.Combine(Path.GetTempPath(), "query-port-fixtures");
        var sources = new[]
        {
            (commonPath, common),
            ("Contracts/Driving/Driving.cs", Driving),
            first,
            second,
            third,
        }.Where(source => source.Item1 is not null);
        var compilation = CSharpCompilation.Create(
            "QueryFixtures",
            sources.Select(source =>
                CSharpSyntaxTree.ParseText(
                    source.Item2,
                    path: Path.Combine(root, source.Item1)
                )
            ),
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        return QueryPortInspection.Inspect(
            compilation,
            "Example.Application",
            root
        );
    }
}
