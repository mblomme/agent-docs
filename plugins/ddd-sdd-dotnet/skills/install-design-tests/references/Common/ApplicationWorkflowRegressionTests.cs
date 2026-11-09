using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Company.BoundedContext.DesignTests.Common;

public class ApplicationWorkflowRegressionTests
{
    private const string Contracts = """
        global using System;
        global using System.Threading.Tasks;
        global using Example.Application.Contracts.Driving;
        global using Example.Application.Contracts.Driven;
        namespace Example.Application.Contracts.Driving {
            public interface IUseCase<T> { Task Execute(T input); }
            public interface IUseCase<T, R> { Task<R> Execute(T input); }
            public interface IQueryUseCase<T, R> { Task<R> Execute(T input); }
            public interface IPolicy<T> { Task Execute(T input); }
        }
        namespace Example.Application.Contracts.Driven {
            public interface IUnitOfWork { Task Do(); }
        }
        """;

    [Fact]
    public void EmptyWorkflowDiscoveryPasses() => Assert.Empty(Inspect());

    [Fact]
    public void ValidCommandsQueriesAndPoliciesPass() =>
        Assert.Empty(
            Inspect(
                (
                    "Create.cs",
                    """
                    namespace Example.Application;
                    public record Input;
                    public sealed class Create : IUseCase<Input> {
                        Task IUseCase<Input>.Execute(Input input) => Task.CompletedTask;
                    }
                    """
                ),
                (
                    "Queries/Find.cs",
                    """
                    namespace Example.Application.Queries;
                    public sealed class Find : IQueryUseCase<Find.Input, System.Collections.Generic.IReadOnlyList<Contracts.Data.Result>> {
                        public class Input {}
                        public Task<System.Collections.Generic.IReadOnlyList<Contracts.Data.Result>> Execute(Input input) => null!;
                    }
                    """
                ),
                (
                    "Policies/Follow.cs",
                    """
                    namespace Example.Application.Policies;
                    public sealed class Follow : IPolicy<string> { public Task Execute(string input) => Task.CompletedTask; }
                    """
                ),
                (
                    "Contracts/Data/Result.cs",
                    "namespace Example.Application.Contracts.Data; public record Result(int Id);"
                )
            )
        );

    [Theory]
    [InlineData("Run.cs", "command-interface")]
    [InlineData("Queries/Run.cs", "query-interface")]
    [InlineData("Queries/Nested/Run.cs", "query-interface")]
    [InlineData("Policies/Run.cs", "policy-interface")]
    public void ExecuteConventionFindsMissingInterfaces(
        string path,
        string rule
    ) =>
        AssertFinding(
            Inspect(
                (
                    path,
                    "namespace Example.Application; public class Run { public Task Execute() => Task.CompletedTask; }"
                )
            ),
            rule,
            "Run",
            path,
            "Implement"
        );

    [Theory]
    [InlineData("Other/Run.cs")]
    [InlineData("WrongName.cs")]
    [InlineData("Queries/Run.cs")]
    public void CommandPlacementUsesPhysicalSource(string path) =>
        AssertFinding(
            Inspect((path, Command)),
            "command-file",
            "Run",
            path,
            "Run.cs"
        );

    [Theory]
    [InlineData(
        "Other/Run.cs",
        "query",
        "IQueryUseCase<Input, int>",
        "Task<int>"
    )]
    [InlineData(
        "Queries/Nested/Run.cs",
        "query",
        "IQueryUseCase<Input, int>",
        "Task<int>"
    )]
    [InlineData("Other/Run.cs", "policy", "IPolicy<Input>", "Task")]
    [InlineData("Policies/Nested/Run.cs", "policy", "IPolicy<Input>", "Task")]
    public void QueryAndPolicyPlacementIsDirect(
        string path,
        string kind,
        string port,
        string result
    ) =>
        AssertFinding(
            Inspect(
                (
                    path,
                    $$"""
                    namespace Example.Application;
                    public class Input {}
                    public class Run : {{port}} { public {{result}} Execute(Input input) => null!; }
                    """
                )
            ),
            kind + "-file",
            "Run",
            path,
            "own"
        );

    [Fact]
    public void BundledWorkflowsAndCrossFilePartialsFail()
    {
        var bundled = Inspect(
            (
                "Run.cs",
                Command
                    + " public class Second : IPolicy<Input> { public Task Execute(Input input) => null!; }"
            )
        );
        AssertFinding(bundled, "command-file", "Run", "Run.cs", "bundle");
        var partial = Inspect(
            (
                "Run.cs",
                Command.Replace(
                    "class Run",
                    "partial class Run",
                    StringComparison.Ordinal
                )
            ),
            (
                "Extra.cs",
                "namespace Example.Application; public partial class Run {}"
            )
        );
        AssertFinding(partial, "command-file", "Run", "Extra.cs", "partial");
    }

    [Theory]
    [InlineData("class Input {}", true)]
    [InlineData("record Input;", true)]
    [InlineData("struct Input {}", false)]
    [InlineData("record struct Input;", false)]
    public void InputMustBeClassOrRecordClass(string declaration, bool valid)
    {
        var findings = Inspect(
            (
                "Run.cs",
                Command.Replace(
                    "class Input {}",
                    declaration,
                    StringComparison.Ordinal
                )
            )
        );
        if (valid)
            Assert.Empty(findings);
        else
            AssertFinding(
                findings,
                "command-input",
                "Input",
                "Run.cs",
                "same file"
            );
    }

    [Fact]
    public void PrimitiveAndSeparateFileInputsFail()
    {
        AssertFinding(
            Inspect(
                (
                    "Run.cs",
                    Command
                        .Replace(
                            "IUseCase<Input>",
                            "IUseCase<int>",
                            StringComparison.Ordinal
                        )
                        .Replace(
                            "Execute(Input input)",
                            "Execute(int input)",
                            StringComparison.Ordinal
                        )
                )
            ),
            "command-input",
            "int",
            "Run.cs",
            "class"
        );
        AssertFinding(
            Inspect(
                (
                    "Run.cs",
                    Command.Replace(
                        "public class Input {}",
                        "",
                        StringComparison.Ordinal
                    )
                ),
                (
                    "Input.cs",
                    "namespace Example.Application; public class Input {}"
                )
            ),
            "command-input",
            "Input",
            "Run.cs",
            "same file"
        );
        AssertFinding(
            Inspect(
                (
                    "Queries/Run.cs",
                    Query.Replace(
                        "public class Input {}",
                        "",
                        StringComparison.Ordinal
                    )
                ),
                (
                    "Input.cs",
                    "namespace Example.Application; public class Input {}"
                )
            ),
            "query-input",
            "Input",
            "Run.cs",
            "same file"
        );
    }

    [Theory]
    [InlineData("IUseCase<Input>", "Task", "Run.cs")]
    [InlineData("IUseCase<Input, int>", "Task<int>", "Run.cs")]
    [InlineData("IQueryUseCase<Input, int>", "Task<int>", "Queries/Run.cs")]
    [InlineData("IPolicy<Input>", "Task", "Policies/Run.cs")]
    public void InheritedInterfacesAndExplicitExecutePass(
        string port,
        string result,
        string path
    ) =>
        Assert.Empty(
            Inspect(
                (
                    path,
                    $$"""
                    namespace Example.Application;
                    public class Input {}
                    public class Run : Base<Input> {}
                    """
                ),
                (
                    "Shared/Base.cs",
                    $$"""
                    namespace Example.Application;
                    public abstract class Base<Input> : {{port}} { {{result}} {{port}}.Execute(Input input) => null!; }
                    """
                )
            )
        );

    [Theory]
    [InlineData("public Run(IUnitOfWork work) {}")]
    [InlineData("private IUnitOfWork? work;")]
    [InlineData("private IUnitOfWork? Work { get; set; }")]
    [InlineData("private object Work() => (IUnitOfWork)null!;")]
    [InlineData("private Task Commit() => ((IUnitOfWork)null!).Do();")]
    [InlineData("private System.Collections.Generic.List<IUnitOfWork>? work;")]
    [InlineData("private void Local() { var work = Factory.Get(); }")]
    [InlineData("public Run(Work work) {}")]
    public void QueryUnitOfWorkSourceDependenciesFail(string member) =>
        AssertFinding(
            Inspect(
                (
                    "Queries/Run.cs",
                    Query.Replace(
                        "public Task<int> Execute",
                        member + " public Task<int> Execute",
                        StringComparison.Ordinal
                    )
                ),
                (
                    "Shared/Work.cs",
                    """
                    namespace Example.Application;
                    public sealed class Work : IUnitOfWork { public Task Do() => Task.CompletedTask; }
                    public static class Factory { public static IUnitOfWork Get() => null!; }
                    """
                )
            ),
            "query-unit-of-work",
            "Run",
            "Queries/Run.cs",
            "read-only"
        );

    [Theory]
    [InlineData("public Run(Example.Domain.Item item) {}")]
    [InlineData("private Example.Domain.Item? item;")]
    [InlineData("private Example.Domain.Item[]? Items { get; set; }")]
    [InlineData(
        "private System.Collections.Generic.List<Example.Domain.Item>? items;"
    )]
    [InlineData("private object Create() => new Example.Domain.Item();")]
    [InlineData(
        "private void Local() { var item = Example.Domain.Item.Create(); }"
    )]
    [InlineData("private int Count() => Example.Domain.Item.Count;")]
    [InlineData("private int Read() => Example.Domain.Item.Read();")]
    [InlineData("private string Name() => nameof(Example.Domain.Item);")]
    [InlineData("private void Generic<T>() where T : Example.Domain.Item {}")]
    [InlineData(
        "private int GenericCall() => Factory.Read<Example.Domain.Item>();"
    )]
    [InlineData("private Alias? item;")]
    public void QueryDomainSourceDependenciesFail(string member) =>
        AssertFinding(
            Inspect(
                (
                    "Queries/Run.cs",
                    "using Alias = Example.Domain.Item;\n"
                        + Query.Replace(
                            "public Task<int> Execute",
                            member + " public Task<int> Execute",
                            StringComparison.Ordinal
                        )
                ),
                (
                    "Shared/Domain.cs",
                    """
                    namespace Example.Domain {
                        public class Item {
                            public static Item Create() => new();
                            public static int Count => 1;
                            public static int Read() => 1;
                        }
                    }
                    namespace Example.Application {
                        public static class Factory { public static int Read<T>() => 1; }
                    }
                    """
                )
            ),
            "query-domain",
            "Run",
            "Queries/Run.cs",
            "read-only"
        );

    [Fact]
    public void InheritedDomainDependencyFails() =>
        AssertFinding(
            Inspect(
                (
                    "Queries/Run.cs",
                    "namespace Example.Application; public class Input {} public class Run : Base<Input> {}"
                ),
                (
                    "Shared/Base.cs",
                    """
                    namespace Example.Application;
                    public abstract class Base<T> : IQueryUseCase<T, int> {
                        protected Example.Domain.Nested.Item? Item { get; set; }
                        public Task<int> Execute(T input) => Task.FromResult(1);
                    }
                    """
                ),
                (
                    "Shared/Domain.cs",
                    "namespace Example.Domain.Nested; public class Item {}"
                )
            ),
            "query-domain",
            "Run",
            "Shared/Base.cs",
            "Inherited"
        );

    [Fact]
    public void CommandDomainAccessAndUnrelatedDomainNamespacesPass() =>
        Assert.Empty(
            Inspect(
                (
                    "Run.cs",
                    Command.Replace(
                        "public Task Execute",
                        "private Example.Domain.Item? item; public Task Execute",
                        StringComparison.Ordinal
                    )
                ),
                (
                    "Queries/Run.cs",
                    Query
                        .Replace(
                            "namespace Example.Application;",
                            "namespace Example.Application.Queries;",
                            StringComparison.Ordinal
                        )
                        .Replace(
                            "public Task<int> Execute",
                            "private Example.DomainModels.Item? item; public Task<int> Execute",
                            StringComparison.Ordinal
                        )
                ),
                (
                    "Shared/Domain.cs",
                    "namespace Example.Domain { public class Item {} } namespace Example.DomainModels { public class Item {} }"
                )
            )
        );

    [Fact]
    public void InheritedUnitOfWorkDependencyFails() =>
        AssertFinding(
            Inspect(
                (
                    "Queries/Run.cs",
                    """
                    namespace Example.Application;
                    public class Input {}
                    public class Run : Base<Input> {}
                    """
                ),
                (
                    "Shared/Base.cs",
                    """
                    namespace Example.Application;
                    public abstract class Base<T> : IQueryUseCase<T, int> {
                        protected IUnitOfWork? Work { get; set; }
                        public Task<int> Execute(T input) => Task.FromResult(1);
                    }
                    """
                )
            ),
            "query-unit-of-work",
            "Run",
            "Shared/Base.cs",
            "Inherited"
        );

    [Fact]
    public void UnrelatedNamesAndCommandUnitOfWorkAreAllowed() =>
        Assert.Empty(
            Inspect(
                (
                    "Run.cs",
                    Command.Replace(
                        "public Task Execute",
                        "public Run(IUnitOfWork work) {} public Task Execute",
                        StringComparison.Ordinal
                    )
                ),
                (
                    "Queries/Run.cs",
                    Query
                        .Replace(
                            "public Task<int> Execute",
                            "private Other.IUnitOfWork? work; public Task<int> Execute",
                            StringComparison.Ordinal
                        )
                        .Replace(
                            "namespace Example.Application;",
                            "namespace Example.Application.Queries;",
                            StringComparison.Ordinal
                        )
                ),
                (
                    "Shared/Other.cs",
                    "namespace Other; public interface IUnitOfWork {}"
                )
            )
        );

    [Theory]
    [InlineData("int")]
    [InlineData("float")]
    [InlineData("double")]
    [InlineData("decimal")]
    [InlineData("nint")]
    [InlineData("nuint")]
    [InlineData("string")]
    [InlineData("Guid")]
    [InlineData("DateOnly?")]
    [InlineData("int[]")]
    [InlineData("System.Collections.Generic.IReadOnlyList<int>")]
    [InlineData(
        "System.Collections.Generic.List<Example.Application.Contracts.Data.Result>"
    )]
    [InlineData("Example.Application.Contracts.Data.Result[]")]
    [InlineData("Example.Application.Contracts.Data.Result?")]
    [InlineData("DayOfWeek")]
    public void SimpleAndCollectionResultsPass(string output) =>
        Assert.Empty(
            Inspect(
                ("Queries/Run.cs", QueryWithOutput(output)),
                (
                    "Contracts/Data/Result.cs",
                    "namespace Example.Application.Contracts.Data; public record struct Result(int Id);"
                )
            )
        );

    [Theory]
    [InlineData("Result.cs", "Example.Application")]
    [InlineData(
        "Contracts/Data/Nested/Result.cs",
        "Example.Application.Contracts.Data"
    )]
    [InlineData("Contracts/Data/Result.cs", "Other")]
    public void CustomResultRequiresPhysicalFolderAndNamespace(
        string path,
        string ns
    ) =>
        AssertFinding(
            Inspect(
                (
                    "Queries/Run.cs",
                    QueryWithOutput(
                        "System.Collections.Generic.IEnumerable<global::"
                            + ns
                            + ".Result>"
                    )
                ),
                (path, "namespace " + ns + "; public record Result;")
            ),
            "query-output",
            "Result",
            "Run.cs",
            "Contracts/Data"
        );

    [Fact]
    public void HelpersAndAbstractBasesAreNotWorkflows() =>
        Assert.Empty(
            Inspect(
                (
                    "Queries/Helper.cs",
                    "namespace Example.Application; public class Helper { public static Task Execute() => null!; }"
                ),
                (
                    "Base.cs",
                    "namespace Example.Application; public abstract class Base { public abstract Task Execute(); }"
                )
            )
        );

    [Fact]
    public void CompilationErrorsAreReported() =>
        AssertFinding(
            Inspect(
                (
                    "Run.cs",
                    "namespace Example.Application; class Run : Missing {}"
                )
            ),
            "analysis",
            "Run.cs",
            "Missing",
            "Repair"
        );

    private static string QueryWithOutput(string output) =>
        Query
            .Replace(
                "IQueryUseCase<Input, int>",
                "IQueryUseCase<Input, " + output + ">",
                StringComparison.Ordinal
            )
            .Replace(
                "Task<int>",
                "Task<" + output + ">",
                StringComparison.Ordinal
            )
            .Replace("Task.FromResult(1)", "null!", StringComparison.Ordinal);

    private const string Command = """
        namespace Example.Application;
        public class Input {}
        public class Run : IUseCase<Input> { public Task Execute(Input input) => Task.CompletedTask; }
        """;
    private const string Query = """
        namespace Example.Application;
        public class Input {}
        public class Run : IQueryUseCase<Input, int> { public Task<int> Execute(Input input) => Task.FromResult(1); }
        """;

    private static readonly MetadataReference[] References = (
        (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!
    )
        .Split(Path.PathSeparator)
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToArray();

    private static IReadOnlyList<ApplicationWorkflowInspection.Violation> Inspect(
        params (string Path, string Text)[] sources
    )
    {
        var root = Path.Combine(Path.GetTempPath(), "workflow-fixtures");
        var compilation = CSharpCompilation.Create(
            "Fixtures",
            sources
                .Prepend((Path: "Contracts.cs", Text: Contracts))
                .Select(source =>
                    CSharpSyntaxTree.ParseText(
                        source.Text,
                        path: Path.Combine(root, source.Path)
                    )
                ),
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        return ApplicationWorkflowInspection.Inspect(
            compilation,
            "Example.Application",
            root
        );
    }

    private static void AssertFinding(
        IReadOnlyList<ApplicationWorkflowInspection.Violation> findings,
        string rule,
        params string[] fragments
    )
    {
        Assert.DoesNotContain(
            findings,
            finding => rule != "analysis" && finding.Rule == "analysis"
        );
        var matching = findings
            .Where(finding => finding.Rule == rule)
            .ToArray();
        Assert.NotEmpty(matching);
        Assert.Contains(
            matching,
            finding =>
                fragments.All(fragment =>
                    finding.Message.Contains(fragment, StringComparison.Ordinal)
                )
        );
    }
}
