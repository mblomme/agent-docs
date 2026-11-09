using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Company.BoundedContext.DesignTests.Common;

public class UseCaseImplementationRegressionTests
{
    [Fact]
    public void AwaitedGenericRepositoryLookupResolvesTheConcreteAggregate() =>
        Assert.Empty(
            Inspect(
                """
                namespace Example;
                public interface IRead<T> { Task<T> ById(); }
                public interface IRepo : IRepository, IRead<A> { }
                public sealed class C(IUnitOfWork u) : IUseCase<bool> {
                    public async Task Execute(bool input) {
                        var root = await u.Repo<IRepo>().ById();
                        await u.Save<IRepo>(root);
                        await u.Do();
                    }
                }
                """
            )
        );

    [Fact]
    public void ForeachDisposalDoesNotHideCommitRequirements()
    {
        const string source = """
            namespace Example;
            public sealed class C(IUnitOfWork u) : IUseCase<bool> {
                public async Task Execute(bool input) {
                    foreach (var item in new System.Collections.Generic.List<A> { new A() }) {
                        await u.Save<IARepository>(item);
                    }
                    COMMIT
                }
            }
            """;
        Assert.Empty(
            Inspect(
                source.Replace(
                    "COMMIT",
                    "await u.Do();",
                    StringComparison.Ordinal
                )
            )
        );
        Assert.Contains(
            Inspect(source.Replace("COMMIT", "", StringComparison.Ordinal)),
            violation => violation.Rule == "commit"
        );
    }

    [Fact]
    public void ExplicitFinallyPersistenceRemainsAnAnalysisFailure() =>
        Assert.Contains(
            Inspect(
                UseCase(
                    "try { await u.Do(); } finally { await u.Save<IARepository>(new A()); }"
                )
            ),
            violation => violation.Rule == "analysis"
        );

    [Fact]
    public void AbstractPortCallsDoNotHideTheCallersCommitRequirement()
    {
        const string source = """
            namespace Example;
            public interface IAuthorization { Task Require(); }
            public sealed class C(IUnitOfWork u, IAuthorization auth) : IUseCase<bool> {
                public async Task Execute(bool input) {
                    await auth.Require();
                    await u.Save<IARepository>(new A());
                    COMMIT
                }
            }
            """;
        Assert.Empty(
            Inspect(
                source.Replace(
                    "COMMIT",
                    "await u.Do();",
                    StringComparison.Ordinal
                )
            )
        );
        Assert.Contains(
            Inspect(source.Replace("COMMIT", "", StringComparison.Ordinal)),
            violation => violation.Rule == "commit"
        );
    }

    [Fact]
    public void DefaultInterfaceHelpersStillRevealAdditionalAggregateSaves() =>
        Assert.Contains(
            Inspect(
                """
                namespace Example;
                public interface IHelper {
                    async Task Save(IUnitOfWork u) { await u.Save<IARepository>(new B()); }
                }
                public sealed class C(IUnitOfWork u, IHelper helper) : IUseCase<bool> {
                    public async Task Execute(bool input) {
                        await u.Save<IARepository>(new A());
                        await helper.Save(u);
                        await u.Do();
                    }
                }
                """
            ),
            violation => violation.Rule is "save" or "analysis"
        );

    private const string Contracts = """
        using System;
        using System.Threading.Tasks;
        namespace Example {
        public interface IRepository {}
        public interface IAggregateRoot {}
        public class A : IAggregateRoot {}
        public class B : IAggregateRoot {}
        public interface IARepository : IRepository {}
        public interface IUnitOfWork {
            Task Save<T>(IAggregateRoot aggregate) where T : IRepository;
            T Repo<T>() where T : IRepository;
            Task Do();
        }
        public interface IUseCase<T> { Task Execute(T input); }
        public interface IUseCase<T, TResult> { Task<TResult> Execute(T input); }
        public interface IQueryUseCase<T, TResult> { Task<TResult> Execute(T input); }
        public interface IPolicy<T> { Task Execute(T input); }
        }
        """;

    [Theory]
    [InlineData("await u.Save<IARepository>(new A()); await u.Do();")]
    [InlineData(
        "await u.Save<IARepository>(new A()); await u.Save<IARepository>(new A()); await u.Do();"
    )]
    [InlineData(
        "if (input) await u.Save<IARepository>(new A()); else await u.Save<IARepository>(new B()); await u.Do();"
    )]
    [InlineData("if (input) throw new Exception(); await u.Do();")]
    [InlineData("var repo = u.Repo<IARepository>(); await u.Do();")]
    [InlineData(
        "while(input) { await u.Save<IARepository>(new A()); } await u.Do();"
    )]
    public void ValidExecutionPathsPass(string body) =>
        Assert.Empty(Inspect(UseCase(body)));

    [Theory]
    [InlineData(
        "await u.Save<IARepository>(new A()); await u.Save<IARepository>(new B()); await u.Do();",
        "save"
    )]
    [InlineData(
        "if(input) await u.Save<IARepository>(new A()); await u.Save<IARepository>(new B()); await u.Do();",
        "save"
    )]
    [InlineData("await Task.CompletedTask;", "commit")]
    [InlineData("if(input) return; await u.Do();", "commit")]
    [InlineData("if(input) await u.Do();", "commit")]
    [InlineData("u.Do(); await Task.CompletedTask;", "commit")]
    [InlineData("await u.Do(); await u.Save<IARepository>(new A());", "commit")]
    [InlineData(
        "IAggregateRoot a = input ? new A() : new B(); await u.Save<IARepository>(a); await u.Save<IARepository>(new A()); await u.Do();",
        "save"
    )]
    public void InvalidExecutionPathsReportRule(string body, string rule) =>
        Assert.Contains(Inspect(UseCase(body)), v => v.Rule == rule);

    [Fact]
    public void DirectTaskReturnAndOutputReturnPass()
    {
        Assert.Empty(
            Inspect(
                """
                namespace Example;
                public sealed class Direct(IUnitOfWork u) : IUseCase<bool> {
                    public Task Execute(bool input) => u.Do();
                }
                public sealed class Output(IUnitOfWork u) : IUseCase<bool, int> {
                    public async Task<int> Execute(bool input) { await u.Do(); return 1; }
                }
                """
            )
        );
    }

    [Theory]
    [InlineData("public C(IARepository repository) {}")]
    [InlineData("public C(Repository repository) {}")]
    public void RepositoryConstructorDependenciesFail(string constructor) =>
        Assert.Contains(
            Inspect(
                $$"""
                namespace Example;
                public class Repository : IARepository {}
                public sealed class C : IUseCase<bool> {
                    {{constructor}}
                    public Task Execute(bool input) => Task.CompletedTask;
                }
                """
            ),
            v => v.Rule == "injection"
        );

    [Fact]
    public void InheritedDependenciesAndExplicitExecuteAreInspected() =>
        Assert.Contains(
            Inspect(
                """
                namespace Example;
                public abstract class Base(IARepository repository) : IUseCase<bool> {
                    Task IUseCase<bool>.Execute(bool input) => Task.CompletedTask;
                }
                public sealed class C(IARepository repository) : Base(repository) {}
                """
            ),
            v => v.Rule == "injection"
        );

    [Fact]
    public void QueryAndPolicyImplementationsAreExcluded() =>
        Assert.Empty(
            Inspect(
                """
                namespace Example;
                public class Query(IARepository repository) : IQueryUseCase<bool, int> {
                    public Task<int> Execute(bool input) => Task.FromResult(1);
                }
                public class Policy(IARepository repository) : IPolicy<bool> {
                    public Task Execute(bool input) => Task.CompletedTask;
                }
                """
            )
        );

    [Theory]
    [InlineData(
        "private async Task SaveOther() { await u.Save<IARepository>(new B()); }",
        "await SaveOther();",
        "save"
    )]
    [InlineData(
        "private A Create() => new A();",
        "IAggregateRoot a = Create(); await u.Save<IARepository>(a);",
        "valid"
    )]
    [InlineData("private Task Commit() => u.Do();", "await Commit();", "valid")]
    public void HelpersPropagatePersistenceAndReturnTypes(
        string helper,
        string call,
        string rule
    )
    {
        var body =
            rule == "save" ? "await u.Save<IARepository>(new A()); " : "";
        var results = Inspect(UseCase(body + call + " await u.Do();", helper));
        if (rule == "valid")
            Assert.Empty(results);
        else
            Assert.Contains(results, v => v.Rule == rule);
    }

    [Fact]
    public void LocalFunctionsAreInspected() =>
        Assert.Empty(
            Inspect(
                UseCase(
                    """
                    A Create() => new A();
                    await u.Save<IARepository>(Create());
                    await u.Do();
                    """
                )
            )
        );

    [Fact]
    public void LocalFunctionSavesCannotHideAnotherAggregateType() =>
        Assert.Contains(
            Inspect(
                UseCase(
                    """
                    async Task SaveOther() { await u.Save<IARepository>(new B()); }
                    await u.Save<IARepository>(new A());
                    await SaveOther();
                    await u.Do();
                    """
                )
            ),
            v => v.Rule == "save"
        );

    [Fact]
    public void LoopCanReachMultipleAggregateTypes() =>
        Assert.Contains(
            Inspect(
                UseCase(
                    """
                    while (input) {
                        if (DateTime.Now.Ticks > 0) await u.Save<IARepository>(new A());
                        else await u.Save<IARepository>(new B());
                    }
                    await u.Do();
                    """
                )
            ),
            v => v.Rule == "save"
        );

    [Fact]
    public void InheritedExecuteIsInspected() =>
        Assert.Empty(
            Inspect(
                """
                namespace Example;
                public abstract class Base(IUnitOfWork u) : IUseCase<bool> {
                    public Task Execute(bool input) => u.Do();
                }
                public sealed class C(IUnitOfWork u) : Base(u) {}
                """
            )
        );

    [Fact]
    public void ExplicitExecuteIsInspected() =>
        Assert.Empty(
            Inspect(
                """
                namespace Example;
                public sealed class C(IUnitOfWork u) : IUseCase<bool> {
                    Task IUseCase<bool>.Execute(bool input) => u.Do();
                }
                """
            )
        );

    [Theory]
    [InlineData("await u.Do().ConfigureAwait(false);")]
    [InlineData("if (input) { await u.Do(); return; } await u.Do();")]
    public void AlternativeCommitFormsPass(string body) =>
        Assert.Empty(Inspect(UseCase(body)));

    [Theory]
    [InlineData("await Again();", "private Task Again() => Again();")]
    [InlineData(
        "await u.Save<IARepository>(Unknown()); await u.Do();",
        "private IAggregateRoot Unknown() => null!;"
    )]
    [InlineData("try { await u.Do(); } catch { return; }", "")]
    public void UnresolvedAnalysisIsExplicit(string body, string helper) =>
        Assert.Contains(
            Inspect(UseCase(body, helper)),
            v => v.Rule == "analysis"
        );

    [Fact]
    public void HelperReturnBranchesRemainSeparate() =>
        Assert.Contains(
            Inspect(
                UseCase(
                    """
                    var a = Create(input);
                    await u.Save<IARepository>(a);
                    await u.Save<IARepository>(new A());
                    await u.Do();
                    """,
                    "private IAggregateRoot Create(bool input) { if (input) return new A(); return new B(); }"
                )
            ),
            v => v.Rule == "save"
        );

    [Fact]
    public void NamedHelperArgumentsPreserveAggregateTypes() =>
        Assert.Contains(
            Inspect(
                UseCase(
                    """
                    await SaveBoth(second: new B(), first: new A());
                    await u.Do();
                    """,
                    """
                    private async Task SaveBoth(IAggregateRoot first, IAggregateRoot second) {
                        await u.Save<IARepository>(first);
                        await u.Save<IARepository>(second);
                    }
                    """
                )
            ),
            v => v.Rule == "save"
        );

    private static string UseCase(string body, string helper = "") =>
        $$"""
            namespace Example;
            public sealed class C(IUnitOfWork u) : IUseCase<bool> {
                public async Task Execute(bool input) { {{body}} }
                {{helper}}
            }
            """;

    private static IReadOnlyList<UseCaseImplementationInspection.Violation> Inspect(
        string source
    )
    {
        var references = (
            (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!
        )
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "Fixtures",
            [
                CSharpSyntaxTree.ParseText(Contracts, path: "Contracts.cs"),
                CSharpSyntaxTree.ParseText(
                    "using System; using System.Threading.Tasks; " + source,
                    path: "Fixture.cs"
                ),
            ],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
        return UseCaseImplementationInspection.Inspect(
            compilation,
            "Example",
            "Example.IRepository",
            "Example.IUnitOfWork",
            "Example.IAggregateRoot"
        );
    }
}
