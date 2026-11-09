using Company.BoundedContext.DesignTests.Common;
using static Company.BoundedContext.DesignTests.Common.DomainTestModel;

namespace Company.BoundedContext.DesignTests;

public class DddTacticalDomainEventsTests
{
    /// <summary>Each source file contains at most one concrete domain event.</summary>
    [Fact]
    [DddRule("DDD_TDE_P001")]
    public void ConcreteDomainEventsAreDeclaredInSeparateFiles() =>
        Scenario
            .Given("Concrete domain event source files")
            .When(
                DomainTypeSourceInspection.OnePerFile(
                    AppContext.BaseDirectory,
                    typeof(DddTacticalDomainEventsTests)
                        .Assembly.GetName()
                        .Name!,
                    DomainAssembly.GetName().Name!,
                    ConcreteDomainEvents,
                    "domain event"
                )
            )
            .ThenNoViolations(
                "Move each concrete domain event into a separate .cs file. Abstract base types do not count."
            );

    /// <summary>Checks the shared domain-event interface and metadata getters.</summary>
    [Fact]
    [DddRule("DDD_TDE_O001")]
    public void ToolkitContainsDomainEventInterfaceWithGetterOnlyMetadata() =>
        Scenario
            .Given("Shared domain event interface")
            .When(DomainEventContractInspection.Interface(DomainEventInterface))
            .ThenNoViolations(
                "Declare public interface IDomainEvent in DddToolkit.Domain with getter-only string FQDN and OccurredOn properties."
            );

    /// <summary>Checks the shared abstract event base implements its contract.</summary>
    [Fact]
    [DddRule("DDD_TDE_O002")]
    public void ToolkitContainsAbstractBaseDomainEventImplementingInterface() =>
        Scenario
            .Given("Shared base domain event")
            .When(
                DomainEventContractInspection.BaseEvent(
                    BaseDomainEvent,
                    DomainEventInterface
                )
            )
            .ThenNoViolations(
                "Declare public abstract class BaseDomainEvent in DddToolkit.Domain and make it implement IDomainEvent."
            );

    /// <summary>Checks every discovered aggregate has its own abstract event base.</summary>
    [Fact]
    [DddRule("DDD_TDE_O003")]
    public void EveryAggregateHasItsOwnAbstractDomainEventBase()
    {
        if (ConcreteAggregateRoots.Length == 0)
            return;
        Scenario
            .Given("Aggregate-specific domain event bases")
            .When(
                DomainEventContractInspection.AggregateEventBases(
                    DomainAssembly,
                    AggregateRoots,
                    BaseDomainEvent,
                    DomainEventInterface
                )
            )
            .ThenNoViolations(
                "For each public concrete aggregate root, declare a public abstract {AggregateName}DomainEvent directly deriving from BaseDomainEvent in the Domain assembly."
            );
    }

    /// <summary>Checks concrete events directly inherit their aggregate event base.</summary>
    [Fact]
    [DddRule("DDD_TDE_H001")]
    public void ConcreteDomainEventsInheritTheirAggregateDomainEventBase()
    {
        if (ConcreteDomainEvents.Length == 0)
            return;
        Scenario
            .Given("Concrete domain event inheritance")
            .When(
                DomainEventContractInspection.ConcreteEventInheritance(
                    ConcreteDomainEvents,
                    DomainEventContractInspection.AggregateEventBaseTypes(
                        DomainAssembly,
                        AggregateRoots
                    ),
                    BaseDomainEvent
                )
            )
            .ThenNoViolations(
                "Make every concrete IDomainEvent class directly inherit its aggregate-specific abstract {AggregateName}DomainEvent, not IDomainEvent or BaseDomainEvent."
            );
    }

    /// <summary>Checks concrete event classes cannot be extended.</summary>
    [Fact]
    [DddRule("DDD_TDE_H002")]
    public void ConcreteDomainEventsAreSealed()
    {
        if (ConcreteDomainEvents.Length == 0)
            return;
        Scenario
            .Given("Concrete domain event sealing")
            .When(
                DomainEventContractInspection.ConcreteEventsAreSealed(
                    ConcreteDomainEvents
                )
            )
            .ThenNoViolations(
                "Declare each concrete domain event as a sealed class or sealed record."
            );
    }

    /// <summary>Checks concrete events declare a static Create factory.</summary>
    [Fact]
    [DddRule("DDD_TDE_O004")]
    public void ConcreteDomainEventsHaveStaticCreateFactoryMethods()
    {
        if (ConcreteDomainEvents.Length == 0)
            return;
        Scenario
            .Given("Concrete domain event factories")
            .When(
                ConcreteDomainEvents
                    .Where(type =>
                        !type.GetMethods(
                                System.Reflection.BindingFlags.Public
                                    | System.Reflection.BindingFlags.NonPublic
                                    | System.Reflection.BindingFlags.Static
                                    | System
                                        .Reflection
                                        .BindingFlags
                                        .DeclaredOnly
                            )
                            .Any(method =>
                                method.Name == "Create"
                                && method.ReturnType == type
                            )
                    )
                    .Select(type =>
                        $"{type.Name}: missing a static Create factory returning {type.Name}."
                    )
            )
            .ThenNoViolations(
                "Domain events must have a static factory method by the name 'Create', use this method only to create domain events and avoid domain event creation by switch statements"
            );
    }

    /// <summary>Checks the event FQDN is composed from four ordered components.</summary>
    [Fact]
    [DddRule("DDD_TDE_N001")]
    public void DomainEventFqdnUsesCompanyContextAggregateAndEventNames() =>
        Scenario
            .Given("Domain event FQDN format")
            .When(
                DomainEventContractInspection.FqdnFormat(
                    BaseDomainEvent,
                    DomainAssembly.GetName().Name![..^".Domain".Length]
                )
            )
            .ThenNoViolations(
                "Build FQDN as companyName + \".\" + boundedContext + \".\" + aggregateName + \".\" + eventName. Keep all four components in that order and separate them with dots."
            );

    private static Type DomainEventInterface =>
        AggregateContractInspection.SharedType(
            ToolkitDomainAssembly,
            "IDomainEvent",
            "Declare public interface IDomainEvent in DddToolkit.Domain."
        );

    private static Type BaseDomainEvent =>
        AggregateContractInspection.SharedType(
            ToolkitDomainAssembly,
            "BaseDomainEvent",
            "Declare public abstract class BaseDomainEvent in DddToolkit.Domain."
        );

    private static Type[] AggregateRoots => ConcreteAggregateRoots;

    private static Type[] ConcreteDomainEvents =>
        DomainTestModel.ConcreteDomainEvents;
}
