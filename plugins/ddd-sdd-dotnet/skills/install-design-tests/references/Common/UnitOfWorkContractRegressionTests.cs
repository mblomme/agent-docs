using Company.BoundedContext.DesignTests.Common.ContractFixtures;
using Company.BoundedContext.DesignTests.Common.UnitOfWorkFixtures;

namespace Company.BoundedContext.DesignTests.Common;

public class UnitOfWorkContractRegressionTests
{
    [Theory]
    [InlineData(typeof(IValid))]
    [InlineData(typeof(IInherited))]
    public void ValidAndInheritedContractsAreAccepted(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Assert.Empty(UnitOfWorkContractInspection.Shape(type));
        Assert.Empty(UnitOfWorkContractInspection.Do(type));
        Assert.Empty(
            UnitOfWorkContractInspection.Save(
                type,
                typeof(IAggregateRoot),
                typeof(IRepository)
            )
        );
        Assert.Empty(
            UnitOfWorkContractInspection.Repo(type, typeof(IRepository))
        );
    }

    [Theory]
    [InlineData(typeof(ISynchronous))]
    [InlineData(typeof(IValueTask))]
    [InlineData(typeof(IWrongArguments))]
    [InlineData(typeof(IWrongArity))]
    [InlineData(typeof(IStatic))]
    [InlineData(typeof(IProtected))]
    [InlineData(typeof(IMissing))]
    public void InvalidOperationsReportExactSignatures(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        AssertViolation(
            UnitOfWorkContractInspection.Do(type),
            type,
            "Task Do()"
        );
        AssertViolation(
            UnitOfWorkContractInspection.Save(
                type,
                typeof(IAggregateRoot),
                typeof(IRepository)
            ),
            type,
            "Task Save<TRepository>(IAggregateRoot aggregate) where TRepository : IRepository"
        );
        AssertViolation(
            UnitOfWorkContractInspection.Repo(type, typeof(IRepository)),
            type,
            "TRepository Repo<TRepository>() where TRepository : IRepository"
        );
    }

    [Theory]
    [InlineData(typeof(IUnconstrained))]
    [InlineData(typeof(IOverconstrained))]
    public void InvalidRepositoryConstraintsAreRejected(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Assert.Contains(
            "where TRepository : IRepository",
            Assert.Single(
                UnitOfWorkContractInspection.Save(
                    type,
                    typeof(IAggregateRoot),
                    typeof(IRepository)
                )
            ),
            StringComparison.Ordinal
        );
        Assert.Contains(
            "where TRepository : IRepository",
            Assert.Single(
                UnitOfWorkContractInspection.Repo(type, typeof(IRepository))
            ),
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(typeof(Concrete))]
    [InlineData(typeof(IGeneric<>))]
    [InlineData(typeof(IHidden))]
    public void InvalidShapesFailWithoutCrashing(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        foreach (
            var inspect in new Func<Type, IEnumerable<string>>[]
            {
                UnitOfWorkContractInspection.Shape,
                UnitOfWorkContractInspection.Do,
                t =>
                    UnitOfWorkContractInspection.Save(
                        t,
                        typeof(IAggregateRoot),
                        typeof(IRepository)
                    ),
                t => UnitOfWorkContractInspection.Repo(t, typeof(IRepository)),
            }
        )
            Assert.Contains(
                "public nongeneric top-level interface",
                Assert.Single(inspect(type)),
                StringComparison.Ordinal
            );
    }

    [Fact]
    public void DiscoveryReportsMissingPortExplicitly()
    {
        var message = Assert.Single(
            UnitOfWorkContractInspection.Contracts(
                [],
                "Example.Application.Contracts.Driven",
                UnitOfWorkContractInspection.Shape
            )
        );
        Assert.Contains(
            "Example.Application.Contracts.Driven.IUnitOfWork",
            message,
            StringComparison.Ordinal
        );
        Assert.Contains("found 0", message, StringComparison.Ordinal);
    }

    [Fact]
    public void DiscoveryAcceptsExactPortAndRejectsWrongNamespace()
    {
        var type = typeof(UnitOfWorkFixtures.IUnitOfWork);
        Assert.Empty(
            UnitOfWorkContractInspection.Contracts(
                [type],
                type.Namespace!,
                UnitOfWorkContractInspection.Shape
            )
        );
        Assert.Single(
            UnitOfWorkContractInspection.Contracts(
                [type],
                "Wrong.Namespace",
                UnitOfWorkContractInspection.Shape
            )
        );
    }

    [Theory]
    [InlineData(
        "Contracts/Driven/Anything.cs",
        "namespace Example.Application.Contracts.Driven; public interface IUnitOfWork {}"
    )]
    [InlineData(
        "Contracts\\Driven\\Anything.cs",
        "namespace Example { namespace Application.Contracts.Driven { public interface IUnitOfWork {} } }"
    )]
    public void SourcePlacementAcceptsActualTopLevelDeclarations(
        string path,
        string source
    ) =>
        Assert.Empty(
            UnitOfWorkContractInspection.DrivenDeclarations(
                [(path, source)],
                "Example.Application.Contracts.Driven"
            )
        );

    [Theory]
    [InlineData("namespace Wrong; public interface IUnitOfWork {}")]
    [InlineData(
        "namespace Example.Application.Contracts.Driven; public class IUnitOfWork {}"
    )]
    [InlineData(
        "namespace Example.Application.Contracts.Driven; public interface IUnitOfWork<T> {}"
    )]
    [InlineData(
        "namespace Example.Application.Contracts.Driven; public class Container { public interface IUnitOfWork {} }"
    )]
    [InlineData("")]
    public void MissingAndWrongSourceDeclarationsAreRejected(string source) =>
        Assert.Contains(
            "missing source declaration",
            Assert.Single(
                UnitOfWorkContractInspection.DrivenDeclarations(
                    [("Contracts/Driven/IUnitOfWork.cs", source)],
                    "Example.Application.Contracts.Driven"
                )
            ),
            StringComparison.Ordinal
        );

    [Fact]
    public void MatchingNamespaceOutsideDrivenFolderFails() =>
        Assert.Contains(
            "move the IUnitOfWork",
            Assert.Single(
                UnitOfWorkContractInspection.DrivenDeclarations(
                    [
                        (
                            "Elsewhere/IUnitOfWork.cs",
                            "namespace Example.Application.Contracts.Driven; public interface IUnitOfWork {}"
                        ),
                    ],
                    "Example.Application.Contracts.Driven"
                )
            ),
            StringComparison.Ordinal
        );

    private static void AssertViolation(
        IEnumerable<string> violations,
        Type type,
        string signature
    )
    {
        var message = Assert.Single(violations);
        Assert.Contains(type.ToString(), message, StringComparison.Ordinal);
        Assert.Contains(signature, message, StringComparison.Ordinal);
    }
}
