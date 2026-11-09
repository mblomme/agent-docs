namespace Company.BoundedContext.DesignTests.Common;

public class RepositoryContractRegressionTests
{
    [Fact]
    public void EmptyMarkersAndTypedRepositoriesWithExtraAndInheritedOperationsAreAccepted()
    {
        Assert.Empty(RepositoryContractInspection.Marker(typeof(IMarker)));
        Assert.Empty(
            RepositoryContractInspection.Marker(typeof(IInheritedEmptyMarker))
        );
        foreach (
            var type in new[] { typeof(IMinimal<,>), typeof(IExtended<,>) }
        )
        {
            Assert.Empty(
                RepositoryContractInspection.TypedInterface(
                    type,
                    typeof(IMarker)
                )
            );
            Assert.Empty(
                RepositoryContractInspection.AggregateConstraint(
                    type,
                    typeof(FixtureRoot<>)
                )
            );
            foreach (var operation in Operations)
                Assert.Empty(
                    RepositoryContractInspection.Operation(type, operation)
                );
        }
        Assert.Empty(
            RepositoryContractInspection.AggregateConstraint(
                typeof(IIndirectConstraint<,>),
                typeof(FixtureRoot<>)
            )
        );
    }

    [Theory]
    [InlineData(typeof(IGenericMarker<>))]
    [InlineData(typeof(IHiddenMarker))]
    [InlineData(typeof(FixtureRoot<>))]
    [InlineData(typeof(IMethodMarker))]
    [InlineData(typeof(IPropertyMarker))]
    [InlineData(typeof(IFieldMarker))]
    [InlineData(typeof(IEventMarker))]
    [InlineData(typeof(IStaticMemberMarker))]
    [InlineData(typeof(IInheritedNonemptyMarker))]
    public void InvalidMarkersReportTheirShapeOrUnexpectedMembers(Type marker)
    {
        ArgumentNullException.ThrowIfNull(marker);
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Repository marker",
                RepositoryContractInspection.Marker(marker),
                "Declare public interface IRepository { } with no declared or inherited members."
            )
        );
        Assert.Contains(marker.Name, failure.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Action: Declare public interface IRepository",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(typeof(IMarker))]
    [InlineData(typeof(IOneParameter<>))]
    [InlineData(typeof(IThreeParameters<,,>))]
    [InlineData(typeof(IHiddenTyped<,>))]
    [InlineData(typeof(FixtureRoot<>))]
    public void TypedInterfaceRejectsWrongKindVisibilityAndGenericArity(
        Type repository
    )
    {
        ArgumentNullException.ThrowIfNull(repository);
        var violations = RepositoryContractInspection.TypedInterface(
            repository,
            typeof(IMarker)
        );
        Assert.Contains(
            violations,
            message =>
                message.Contains(
                    "exactly two generic parameters",
                    StringComparison.Ordinal
                )
        );
    }

    [Fact]
    public void TypedInterfaceMustInheritTheActualMarker()
    {
        var violation = Assert.Single(
            RepositoryContractInspection.TypedInterface(
                typeof(INoMarker<,>),
                typeof(IMarker)
            )
        );
        Assert.Contains("must inherit", violation, StringComparison.Ordinal);
        Assert.Contains("IMarker", violation, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(typeof(INoMarker<,>))]
    [InlineData(typeof(IFixedConstraint<,>))]
    [InlineData(typeof(ISwappedConstraint<,>))]
    public void AggregateConstraintMustConnectFirstParameterToSecondId(
        Type repository
    )
    {
        ArgumentNullException.ThrowIfNull(repository);
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Repository aggregate constraint",
                RepositoryContractInspection.AggregateConstraint(
                    repository,
                    typeof(FixtureRoot<>)
                ),
                "Add where TAggregate : AggregateRoot<TId> using the second parameter as TId."
            )
        );
        Assert.Contains(
            repository.Name,
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "second parameter TId",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Action: Add where TAggregate",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(typeof(INoMarker<,>))]
    [InlineData(typeof(IWrongInputs<,>))]
    [InlineData(typeof(IWrongReturns<,>))]
    [InlineData(typeof(IStaticOperations<,>))]
    [InlineData(typeof(IGenericOperations<,>))]
    [InlineData(typeof(INonPublicOperations<,>))]
    public void MissingOrIncorrectOperationsReportExpectedSignatureAndFix(
        Type repository
    )
    {
        ArgumentNullException.ThrowIfNull(repository);
        foreach (var operation in Operations)
        {
            var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
                DiagnosticAssertions.AssertNoViolations(
                    "Repository operation",
                    RepositoryContractInspection.Operation(
                        repository,
                        operation
                    ),
                    "Add the required public Task-based repository operation."
                )
            );
            Assert.Contains(
                repository.Name + "." + operation,
                failure.Message,
                StringComparison.Ordinal
            );
            Assert.Contains("Task", failure.Message, StringComparison.Ordinal);
            Assert.Contains(
                "allowing extra parameters",
                failure.Message,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Action: Add the required",
                failure.Message,
                StringComparison.Ordinal
            );
        }
    }

    [Fact]
    public void MalformedTypedInterfaceProducesAnAssertionInsteadOfAnIndexError()
    {
        foreach (var operation in Operations)
            Assert.Contains(
                "exactly two generic parameters",
                Assert.Single(
                    RepositoryContractInspection.Operation(
                        typeof(IOneParameter<>),
                        operation
                    )
                ),
                StringComparison.Ordinal
            );
    }

    [Fact]
    public void MissingSharedMarkerProducesAnActionableDiscoveryFailure()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            AggregateContractInspection.SharedType(
                typeof(RepositoryContractRegressionTests).Assembly,
                "IRepository",
                "Declare public interface IRepository { } in DddToolkit.Domain."
            )
        );
        Assert.Contains(
            "IRepository",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains("found 0", failure.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Action: Declare public interface IRepository",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(
        "Orders/IOrders.cs",
        "namespace Domain.Orders; interface IOrders {}",
        true
    )]
    [InlineData(
        "Shared/IOrders.cs",
        "namespace Domain.Orders; interface IOrders {}",
        false
    )]
    [InlineData(
        "Orders/Order.cs",
        "namespace Domain.Orders; interface IOrders {}",
        false
    )]
    [InlineData(
        "Orders/IOrders.cs",
        "namespace Domain.Orders; interface IOrders {} interface IOther {}",
        false
    )]
    public void RepositoryPlacementUsesPhysicalFoldersAndSeparateFiles(
        string path,
        string text,
        bool valid
    )
    {
        var violations = DomainTypeSourceInspection
            .RepositoriesBesideAggregates(
                [("Domain.Orders.IOrders", "Domain.Orders.Order")],
                [
                    (
                        "Orders/Order.cs",
                        "namespace Domain.Orders; class Order {}"
                    ),
                    (path, text),
                ]
            )
            .ToArray();
        Assert.Equal(valid, violations.Length == 0);
    }

    [Fact]
    public void RepositoryPlacementReportsMissingSource()
    {
        Assert.NotEmpty(
            DomainTypeSourceInspection.RepositoriesBesideAggregates(
                [("Domain.Orders.IOrders", "Domain.Orders.Order")],
                []
            )
        );
        Assert.Empty(
            DomainTypeSourceInspection.RepositoriesBesideAggregates([], [])
        );
    }

    private static readonly string[] Operations =
    [
        "Exists",
        "ById",
        "Save",
        "Remove",
    ];

    // Public nested fixtures exercise reflection visibility; deliberately invalid
    // repository contracts stay isolated from production and its discovery configuration.
#pragma warning disable CA1034, CA1515
    public interface IMarker;

    public interface IInheritedEmptyMarker : IMarker;

    public interface IGenericMarker<T>;

    private interface IHiddenMarker;

    public interface IMethodMarker
    {
        void Change();
    }

    public interface IPropertyMarker
    {
        int Count { get; }
    }

    public interface IFieldMarker
    {
        const int Flag = 1;
    }

    public interface IEventMarker
    {
        event EventHandler Changed;
    }

    public interface IStaticMemberMarker
    {
        static void Change() { }
    }

    public interface IInheritedNonemptyMarker : IMethodMarker;

    public abstract class FixtureRoot<TId>;

    public abstract class FixtureIntermediate<TId> : FixtureRoot<TId>;

    public interface IMinimal<TAggregate, TId> : IMarker
        where TAggregate : FixtureRoot<TId>
    {
        Task<bool> Exists(TId id);
        Task<TAggregate?> ById(TId id);
        Task Save(TAggregate aggregate);
        Task Remove(TAggregate aggregate);
    }

    public interface IOperations<TAggregate, TId>
    {
        Task<bool> Exists(
            string owner,
            TId id,
            CancellationToken cancellationToken
        );
        Task<TAggregate> ById(TId id, CancellationToken cancellationToken);
        Task Save(TAggregate aggregate, long expectedVersion);
        Task Remove(string reason, TAggregate aggregate);
    }

    public interface IExtended<TAggregate, TId>
        : IOperations<TAggregate, TId>,
            IInheritedEmptyMarker
        where TAggregate : FixtureRoot<TId>
    {
        string Description { get; }
        Task<bool> Exists(int unrelated);
        Task Archive(TAggregate aggregate);
    }

    public interface IIndirectConstraint<TAggregate, TId> : IMarker
        where TAggregate : FixtureIntermediate<TId>;

    public interface IOneParameter<T> : IMarker;

    public interface IThreeParameters<TAggregate, TId, TOther> : IMarker;

    private interface IHiddenTyped<TAggregate, TId> : IMarker;

    public interface INoMarker<TAggregate, TId>;

    public interface IFixedConstraint<TAggregate, TId>
        where TAggregate : FixtureRoot<Guid>;

    public interface ISwappedConstraint<TAggregate, TId>
        where TId : FixtureRoot<TAggregate>;

    public interface IWrongInputs<TAggregate, TId>
    {
        Task<bool> Exists(out TId id);
        Task<TAggregate> ById(TAggregate aggregate);
        Task Save(TId id);
        Task Remove(ref TAggregate aggregate);
    }

    public interface IWrongReturns<TAggregate, TId>
    {
        ValueTask<bool> Exists(TId id);
        Task<TId> ById(TId id);
        Task<bool> Save(TAggregate aggregate);
        ValueTask Remove(TAggregate aggregate);
    }

    // Deliberately invalid static operations must remain available to the inspector.
#pragma warning disable CA1000
    public interface IStaticOperations<TAggregate, TId>
    {
        static Task<bool> Exists(TId id) => throw new NotSupportedException();
        static Task<TAggregate> ById(TId id) =>
            throw new NotSupportedException();
        static Task Save(TAggregate aggregate) =>
            throw new NotSupportedException();
        static Task Remove(TAggregate aggregate) =>
            throw new NotSupportedException();
    }

#pragma warning restore CA1000
    public interface IGenericOperations<TAggregate, TId>
    {
        Task<bool> Exists<TOther>(TId id);
        Task<TAggregate> ById<TOther>(TId id);
        Task Save<TOther>(TAggregate aggregate);
        Task Remove<TOther>(TAggregate aggregate);
    }

    public interface INonPublicOperations<TAggregate, TId>
    {
        protected Task<bool> Exists(TId id);
        protected Task<TAggregate> ById(TId id);
        protected Task Save(TAggregate aggregate);
        protected Task Remove(TAggregate aggregate);
    }
#pragma warning restore CA1034, CA1515
}
