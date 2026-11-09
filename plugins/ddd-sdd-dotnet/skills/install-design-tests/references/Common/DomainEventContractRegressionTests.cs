namespace Company.BoundedContext.DesignTests.Common;

public class DomainEventContractRegressionTests
{
    [Fact]
    public void EmptyAggregateDiscoveryDoesNotRequireAggregateEventBases() =>
        Assert.Empty(
            DomainEventContractInspection.AggregateEventBases(
                typeof(DomainEventContractRegressionTests).Assembly,
                [],
                typeof(SharedDomainEventFixture),
                typeof(ValidDomainEventContract)
            )
        );

    [Fact]
    public void EmptyEventDiscoveryDoesNotRequireInheritanceOrSealing()
    {
        Assert.Empty(
            DomainEventContractInspection.ConcreteEventInheritance(
                [],
                [],
                typeof(SharedDomainEventFixture)
            )
        );
        Assert.Empty(DomainEventContractInspection.ConcreteEventsAreSealed([]));
    }

    [Fact]
    public void ConcreteAggregateStillRequiresItsNamedEventBase() =>
        Assert.NotEmpty(
            DomainEventContractInspection.AggregateEventBases(
                typeof(DomainEventContractRegressionTests).Assembly,
                [typeof(DomainExceptionFixture)],
                typeof(SharedDomainEventFixture),
                typeof(ValidDomainEventContract)
            )
        );

    [Fact]
    public void ExceptionContractAcceptsDirectExceptionSubclass()
    {
        Assert.Empty(
            DomainEventContractInspection.DomainException(
                typeof(DomainExceptionFixture)
            )
        );
    }

    [Fact]
    public void InvalidExceptionBaseReportsExpectedContract()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Shared domain exception",
                DomainEventContractInspection.DomainException(
                    typeof(InvalidDomainExceptionFixture)
                ),
                "Declare public class DomainException : Exception in DddToolkit.Domain."
            )
        );

        Assert.Contains(
            "directly extending System.Exception",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Action: Declare public class DomainException : Exception",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void InterfaceContractAcceptsGetterOnlyMetadata()
    {
        Assert.Empty(
            DomainEventContractInspection.Interface(
                typeof(ValidDomainEventContract)
            )
        );
    }

    [Fact]
    public void InterfaceContractReportsMissingAndWritableMetadata()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Shared domain event interface",
                DomainEventContractInspection.Interface(
                    typeof(InvalidDomainEventContract)
                ),
                "Declare getter-only string FQDN and DateTimeOffset OccurredOn properties."
            )
        );

        Assert.Contains(
            "getter-only String FQDN",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "getter-only DateTimeOffset OccurredOn",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void AggregateEventBaseMustBeNamedAndExtendSharedBase()
    {
        var violations = DomainEventContractInspection.AggregateEventBases(
            typeof(DomainEventContractRegressionTests).Assembly,
            [typeof(GameAggregateFixture)],
            typeof(SharedDomainEventFixture),
            typeof(ValidDomainEventContract)
        );

        Assert.Empty(violations);
    }

    [Fact]
    public void ConcreteEventMustDirectlyInheritAggregateEventBase()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Concrete domain event inheritance",
                DomainEventContractInspection.ConcreteEventInheritance(
                    [
                        typeof(GameCreatedEventFixture),
                        typeof(GameWrongBaseEventFixture),
                        typeof(GameStructEventFixture),
                    ],
                    [typeof(GameAggregateFixtureDomainEvent)],
                    typeof(SharedDomainEventFixture)
                ),
                "Make each concrete event directly inherit its aggregate-specific abstract event base."
            )
        );

        Assert.Contains(
            "GameWrongBaseEventFixture",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "aggregate-specific abstract [AggregateName]DomainEvent",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "GameStructEventFixture",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ConcreteEventsMustBeSealed()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Concrete domain event sealing",
                DomainEventContractInspection.ConcreteEventsAreSealed([
                    typeof(GameCreatedEventFixture),
                    typeof(GameMutableEventFixture),
                ]),
                "Declare each concrete event as a sealed class or sealed record."
            )
        );

        Assert.Contains(
            "GameMutableEventFixture",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "must be sealed",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void FqdnContractAcceptsContextAndEventComponents()
    {
        Assert.Empty(
            DomainEventContractInspection.FqdnFormat(
                typeof(ToolkitEventFixtures.ValidBaseDomainEventFixture),
                "Example.Company"
            )
        );
    }

    [Fact]
    public void FqdnContractAcceptsFourDynamicComponents()
    {
        Assert.Empty(
            DomainEventContractInspection.FqdnFormat(
                typeof(ToolkitEventFixtures.FourComponentBaseDomainEventFixture)
            )
        );
    }

    [Fact]
    public void TwoNameEventConstructorRequiresExplicitProductContext() =>
        Assert.Contains(
            DomainEventContractInspection.FqdnFormat(
                typeof(ToolkitEventFixtures.ValidBaseDomainEventFixture)
            ),
            message =>
                message.Contains(
                    "supply the product company",
                    StringComparison.Ordinal
                )
        );

    [Fact]
    public void InvalidFqdnReportsExpectedAndActualValues()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Domain event FQDN format",
                DomainEventContractInspection.FqdnFormat(
                    typeof(ToolkitEventFixtures.InvalidBaseDomainEventFixture),
                    "Example.Company"
                ),
                "Compose company, bounded context, aggregate, and event names in order, separated by dots."
            )
        );

        Assert.Contains(
            "expected \"Example.Company.AggregateProbe.EventProbe\"",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "received \"EventProbe.AggregateProbe.Example.Company\"",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    // These deliberately minimal and malformed types exercise contract diagnostics.
#pragma warning disable CA1032, CA1034, CA1515, CA1710, CA1715, CA1815
    public sealed class DomainExceptionFixture : Exception;

    public sealed class InvalidDomainExceptionFixture
        : InvalidOperationException;

    public interface ValidDomainEventContract
    {
        string FQDN { get; }
        DateTimeOffset OccurredOn { get; }
    }

    public interface InvalidDomainEventContract
    {
        string FQDN { get; set; }
        DateTime OccurredOn { get; }
    }

    public abstract class SharedDomainEventFixture : ValidDomainEventContract
    {
        public string FQDN { get; } = "";
        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UnixEpoch;

        protected SharedDomainEventFixture() { }
    }

    public sealed class GameAggregateFixture;

    public abstract class GameAggregateFixtureDomainEvent
        : SharedDomainEventFixture;

    public sealed class GameCreatedEventFixture
        : GameAggregateFixtureDomainEvent;

    public sealed class GameWrongBaseEventFixture : SharedDomainEventFixture;

    public class GameMutableEventFixture : GameAggregateFixtureDomainEvent;

    public readonly struct GameStructEventFixture : ValidDomainEventContract
    {
        public string FQDN => "";
        public DateTimeOffset OccurredOn => DateTimeOffset.UnixEpoch;
    }
#pragma warning restore CA1032, CA1034, CA1515, CA1710, CA1715, CA1815
}
