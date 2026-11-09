namespace Company.BoundedContext.DesignTests.Common;

public class AggregateContractRegressionTests
{
    [Fact]
    public void ValidContractAndBaseWithInheritedGenericIdAreAccepted()
    {
        Assert.Empty(
            AggregateContractInspection.Interface(typeof(IValidContract))
        );
        Assert.Empty(
            AggregateContractInspection.BaseClass(
                typeof(ValidRoot<>),
                typeof(IValidContract)
            )
        );
        Assert.Empty(AggregateContractInspection.Identity(typeof(ValidRoot<>)));
        Assert.Empty(
            AggregateContractInspection.Version(typeof(IValidContract))
        );
        Assert.Empty(
            AggregateContractInspection.Events(
                typeof(IValidContract),
                typeof(ITestFact)
            )
        );
        Assert.Empty(
            AggregateContractInspection.Events(
                typeof(ValidRoot<>),
                typeof(ITestFact)
            )
        );
        Assert.Empty(AggregateContractInspection.Clear(typeof(IValidContract)));
        Assert.Empty(AggregateContractInspection.Clear(typeof(ValidRoot<>)));
        Assert.Empty(
            AggregateContractInspection.Raise(
                typeof(ValidRoot<>),
                typeof(ITestFact)
            )
        );
        Assert.Empty(
            AggregateContractInspection.Events(
                typeof(IListContract),
                typeof(ITestFact)
            )
        );
    }

    [Fact]
    public void MissingMembersReportSignaturesAndConcreteRemediation()
    {
        var violations = AggregateContractInspection
            .Version(typeof(IEmptyContract))
            .Concat(
                AggregateContractInspection.Events(
                    typeof(IEmptyContract),
                    typeof(ITestFact)
                )
            )
            .Concat(AggregateContractInspection.Clear(typeof(IEmptyContract)))
            .Concat(
                AggregateContractInspection.Raise(
                    typeof(EmptyRoot),
                    typeof(ITestFact)
                )
            );
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Aggregate contract",
                violations,
                "Add the missing aggregate contract members."
            )
        );
        Assert.Contains(
            "IEmptyContract.ConcurrencyVersion",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "IEmptyContract.DomainEvents()",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "IEmptyContract.ClearDomainEvents()",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "EmptyRoot.RaiseDomainEvent",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Action: Add the missing aggregate contract members.",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void InvalidVisibilityInheritanceAndBaseClassShapeAreRejected()
    {
        Assert.NotEmpty(
            AggregateContractInspection.Interface(typeof(IHiddenContract))
        );
        Assert.NotEmpty(
            AggregateContractInspection.Interface(typeof(EmptyRoot))
        );
        Assert.NotEmpty(
            AggregateContractInspection.BaseClass(
                typeof(EmptyRoot),
                typeof(IValidContract)
            )
        );
        Assert.NotEmpty(
            AggregateContractInspection.BaseClass(
                typeof(HiddenRoot),
                typeof(IValidContract)
            )
        );
        Assert.NotEmpty(
            AggregateContractInspection.Raise(
                typeof(PublicRaiseRoot),
                typeof(ITestFact)
            )
        );
        Assert.NotEmpty(
            AggregateContractInspection.Raise(
                typeof(StaticRaiseRoot),
                typeof(ITestFact)
            )
        );
    }

    [Fact]
    public void WrongIdentityAndGenericArityAreRejected()
    {
        Assert.NotEmpty(
            AggregateContractInspection.Identity(typeof(EmptyRoot))
        );
        Assert.NotEmpty(
            AggregateContractInspection.Identity(typeof(TwoIds<,>))
        );
        Assert.NotEmpty(
            AggregateContractInspection.Identity(typeof(WrongId<>))
        );
        Assert.NotEmpty(
            AggregateContractInspection.Identity(typeof(PrivateId<>))
        );
    }

    [Fact]
    public void MutableCollectionsPropertiesWrongEventTypesAndIncorrectSignaturesAreRejected()
    {
        Assert.NotEmpty(
            AggregateContractInspection.Events(
                typeof(IMutableEvents),
                typeof(ITestFact)
            )
        );
        Assert.NotEmpty(
            AggregateContractInspection.Events(
                typeof(IEventProperty),
                typeof(ITestFact)
            )
        );
        Assert.NotEmpty(
            AggregateContractInspection.Events(
                typeof(IListContract),
                typeof(IOtherFact)
            )
        );
        Assert.NotEmpty(
            AggregateContractInspection.Events(
                typeof(IInvalidSignatures),
                typeof(ITestFact)
            )
        );
        Assert.NotEmpty(
            AggregateContractInspection.Clear(typeof(IInvalidSignatures))
        );
        Assert.NotEmpty(
            AggregateContractInspection.Version(typeof(IWritableVersion))
        );
    }

    [Fact]
    public void MissingDiscoveredTypesProduceActionableAssertions()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            AggregateContractInspection.SharedType(
                typeof(AggregateContractRegressionTests).Assembly,
                "IAggregateRoot"
            )
        );
        Assert.Contains(
            "IAggregateRoot",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains("found 0", failure.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Action: Declare",
            failure.Message,
            StringComparison.Ordinal
        );
        var eventFailure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            AggregateContractInspection.DomainEventType(typeof(object).Assembly)
        );
        Assert.Contains(
            "IDomainEvent",
            eventFailure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Action: Declare",
            eventFailure.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void EntityInheritanceAcceptsDirectAndIndirectBasesWithMatchingIdentity()
    {
        Assert.Empty(
            AggregateContractInspection.EntityInheritance(
                typeof(ValidRoot<>),
                typeof(IdentityBase<>)
            )
        );
        Assert.Empty(
            AggregateContractInspection.EntityInheritance(
                typeof(IndirectRoot<>),
                typeof(IdentityBase<>)
            )
        );
    }

    [Fact]
    public void EntityInheritanceRejectsMissingBasesAndWrongIdentityWithGuidance()
    {
        Assert.NotEmpty(
            AggregateContractInspection.EntityInheritance(
                typeof(WrongId<>),
                typeof(IdentityBase<>)
            )
        );
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Aggregate root entity inheritance",
                AggregateContractInspection.EntityInheritance(
                    typeof(FixedIdentityRoot<>),
                    typeof(IdentityBase<>)
                ),
                "Make AggregateRoot<TId> inherit from Entity<TId>, preserving the same TId."
            )
        );
        Assert.Contains(
            "FixedIdentityRoot",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "own ID type parameter",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Action: Make AggregateRoot<TId> inherit from Entity<TId>",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    // These nested fixtures must be publicly visible to exercise Type.IsVisible.
    // Keep them beside the tests rather than exposing reusable application APIs.
#pragma warning disable CA1034, CA1515
    public interface ITestFact;

    public interface IOtherFact;

    public interface IEmptyContract;

    private interface IHiddenContract;

    public interface IValidContract
    {
        long ConcurrencyVersion { get; }
        IReadOnlyCollection<ITestFact> DomainEvents();
        void ClearDomainEvents();
    }

    public interface IListContract
    {
        IReadOnlyList<ITestFact> DomainEvents();
    }

    public class IdentityBase<T>
    {
        public T Id { get; private set; } = default!;
    }

    public abstract class ValidRoot<T> : IdentityBase<T>, IValidContract
    {
        public long ConcurrencyVersion { get; private set; }

        public IReadOnlyCollection<ITestFact> DomainEvents() =>
            Array.Empty<ITestFact>();

        public void ClearDomainEvents() { }

        protected void RaiseDomainEvent(ITestFact fact) { }
    }

    public abstract class IndirectRoot<T> : ValidRoot<T>;

    public abstract class FixedIdentityRoot<T> : IdentityBase<Guid>;

    public class EmptyRoot;

    private abstract class HiddenRoot;

    public abstract class TwoIds<T, TOther>;

    public abstract class WrongId<T>
    {
        public Guid Id { get; }
    }

    public abstract class PrivateId<T>
    {
        private T Id { get; } = default!;
    }

    public abstract class PublicRaiseRoot
    {
        public ITestFact? LastEvent { get; private set; }

        public void RaiseDomainEvent(ITestFact fact) => LastEvent = fact;
    }

    public abstract class StaticRaiseRoot
    {
        protected static void RaiseDomainEvent(ITestFact fact) { }
    }

    public interface IMutableEvents
    {
        // Deliberately mutable return type: the inspection must reject this contract.
#pragma warning disable CA1002
        List<ITestFact> DomainEvents();
#pragma warning restore CA1002
    }

    public interface IEventProperty
    {
        IReadOnlyCollection<ITestFact> DomainEvents { get; }
    }

    public interface IInvalidSignatures
    {
        IReadOnlyCollection<ITestFact> DomainEvents(int ignored);
        int ClearDomainEvents();
    }

    public interface IWritableVersion
    {
        long ConcurrencyVersion { get; set; }
    }
#pragma warning restore CA1034, CA1515
}
