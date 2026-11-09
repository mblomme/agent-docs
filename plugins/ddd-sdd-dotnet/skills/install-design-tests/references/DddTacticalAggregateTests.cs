using Company.BoundedContext.DesignTests.Common;
using static Company.BoundedContext.DesignTests.Common.DomainTestModel;

namespace Company.BoundedContext.DesignTests;

public class DddTacticalAggregateTests
{
    /// <summary>
    /// Checks that aggregates do not depend on another aggregate's child entities.
    /// This preserves each root's control over its consistency boundary.
    /// DDD principle: The aggregate root controls access to its internal entities.
    /// </summary>
    [Fact]
    [DddRule("DDD_TA_B001")]
    public void AggregateRootsProtectInternalEntitiesFromOtherAggregates() =>
        Scenario
            .Given("discovered aggregate boundaries")
            .When(() =>
                DomainModelInspection.AggregateBoundaries(
                    [DomainAssembly],
                    Aggregates
                )
            )
            .ThenCompletesWithoutViolations();

    /// <summary>
    /// Checks that cross-aggregate dependencies use identities instead of root objects.
    /// This enforces the chosen policy of keeping aggregates independently managed.
    /// DDD principle: Reference other aggregates by identity to preserve independent boundaries.
    /// </summary>
    [Fact]
    [DddRule("DDD_TA_R001")]
    public void AggregatesReferenceOtherAggregatesByIdentityRatherThanObjectNavigation() =>
        Scenario
            .Given("cross-aggregate references")
            .When(() =>
                DomainModelInspection.IdentityReferences(
                    [DomainAssembly],
                    Aggregates
                )
            )
            .ThenCompletesWithoutViolations();

    [Fact]
    [DddRule("DDD_TA_O001")]
    public void DomainUsesToolkitWithoutSharedFolder() =>
        Scenario
            .Given("Domain Shared folder")
            .When(
                DomainProjectInspection.ToolkitSources(
                    AppContext.BaseDirectory,
                    typeof(DddTacticalAggregateTests).Assembly.GetName().Name!,
                    DomainAssembly.GetName().Name!
                )
            )
            .ThenNoViolations(
                "Create lib/DddToolkit.Domain and remove Domain/Shared."
            );

    [Fact]
    [DddRule("DDD_TA_O002")]
    public void ToolkitContainsPublicAggregateRootInterface() =>
        Scenario
            .Given("Aggregate root interface")
            .When(AggregateContractInspection.Interface(Contract))
            .ThenNoViolations(
                "Declare public interface IAggregateRoot in DddToolkit.Domain."
            );

    [Fact]
    [DddRule("DDD_TA_C001")]
    public void ToolkitContainsAbstractAggregateRootImplementingContract() =>
        Scenario
            .Given("Aggregate root base class")
            .When(AggregateContractInspection.BaseClass(Root, Contract))
            .ThenNoViolations(
                "Declare public abstract class AggregateRoot<TId> implementing IAggregateRoot in DddToolkit.Domain."
            );

    [Fact]
    [DddRule("DDD_TA_C002")]
    public void AggregateRootUsesItsGenericParameterForIdentity() =>
        Scenario
            .Given("Aggregate root identity")
            .When(AggregateContractInspection.Identity(Root))
            .ThenNoViolations(
                "Give AggregateRoot<TId> exactly one ID parameter and expose public TId Id { get; }, directly or by inheritance."
            );

    [Fact]
    [DddRule("DDD_TA_O003")]
    public void AggregateRootInterfaceExposesConcurrencyVersion() =>
        Scenario
            .Given("Concurrency version contract")
            .When(AggregateContractInspection.Version(Contract))
            .ThenNoViolations(
                "Add long ConcurrencyVersion { get; } to IAggregateRoot."
            );

    [Fact]
    [DddRule("DDD_TA_O004")]
    public void AggregateRootInterfaceExposesDomainEventsQuery() =>
        Scenario
            .Given("Domain events interface query")
            .When(AggregateContractInspection.Events(Contract, EventType))
            .ThenNoViolations(
                "Add IReadOnlyCollection<IDomainEvent> DomainEvents() to IAggregateRoot."
            );

    [Fact]
    [DddRule("DDD_TA_O005")]
    public void AggregateRootInterfaceExposesClearDomainEvents() =>
        Scenario
            .Given("Clear domain events contract")
            .When(AggregateContractInspection.Clear(Contract))
            .ThenNoViolations(
                "Add void ClearDomainEvents() to IAggregateRoot."
            );

    [Fact]
    [DddRule("DDD_TA_D001")]
    public void AggregateRootProvidesProtectedEventRecording() =>
        Scenario
            .Given("Protected event recording")
            .When(AggregateContractInspection.Raise(Root, EventType))
            .ThenNoViolations(
                "Add protected void RaiseDomainEvent(IDomainEvent domainEvent) to AggregateRoot<TId>."
            );

    [Fact]
    [DddRule("DDD_TA_D002")]
    public void AggregateRootProvidesPublicDomainEventsQuery() =>
        Scenario
            .Given("Domain events base query")
            .When(AggregateContractInspection.Events(Root, EventType))
            .ThenNoViolations(
                "Implement public IReadOnlyCollection<IDomainEvent> DomainEvents() on AggregateRoot<TId>; replace the property with a method."
            );

    [Fact]
    [DddRule("DDD_TA_D003")]
    public void AggregateRootProvidesPublicEventClearing() =>
        Scenario
            .Given("Domain events base clearing")
            .When(AggregateContractInspection.Clear(Root))
            .ThenNoViolations(
                "Implement public void ClearDomainEvents() on AggregateRoot<TId>."
            );

    /// <summary>
    /// Checks that the aggregate root is an entity with the same identity type.
    /// DDD principle: An aggregate root is a special type of entity.
    /// </summary>
    [Fact]
    [DddRule("DDD_TA_C003")]
    public void AggregateRootInheritsEntityWithTheSameIdentityType() =>
        Scenario
            .Given("Aggregate root entity inheritance")
            .When(
                AggregateContractInspection.EntityInheritance(
                    Root,
                    AggregateContractInspection.SharedType(
                        ToolkitDomainAssembly,
                        "Entity`1"
                    )
                )
            )
            .ThenNoViolations(
                "Make AggregateRoot<TId> inherit from the DddToolkit.Domain Entity<TId> base class, directly or through an intermediate base class, preserving the same TId."
            );

    [Fact]
    [DddRule("DDD_TA_O006")]
    public void EveryAggregateHasAnAssertionClass()
    {
        if (ConcreteAggregateRoots.Length == 0)
            return;

        Scenario
            .Given("Aggregate assertion classes")
            .When(
                AggregateAssertionInspection.Classes(
                    DomainAssembly,
                    DiscoveredRoots
                )
            )
            .ThenNoViolations(
                "Declare a non-generic <AggregateName>Asserts class in each aggregate's namespace; empty classes are allowed."
            );
    }

    [Fact]
    [DddRule("DDD_TA_O007")]
    public void AggregateAssertionSourcesLiveBesideTheirRoots() =>
        Scenario
            .Given("Aggregate assertion source placement")
            .When(
                DomainProjectInspection.AggregateAssertions(
                    AppContext.BaseDirectory,
                    typeof(DddTacticalAggregateTests).Assembly.GetName().Name!,
                    DomainAssembly.GetName().Name!,
                    DiscoveredRoots
                )
            )
            .ThenNoViolations(
                "Place <AggregateName>Asserts.cs beside <AggregateName>.cs in the aggregate's namespace-relative folder."
            );

    [Fact]
    [DddRule("DDD_TA_N001")]
    public void AggregateAssertionMethodsHaveDescriptiveEnsureNames() =>
        Scenario
            .Given("Aggregate assertion method naming")
            .When(
                AggregateAssertionInspection.Naming(
                    DomainAssembly,
                    DiscoveredRoots
                )
            )
            .ThenNoViolations(
                "Name public and internal assertion methods Ensure followed by a PascalCase description; private helpers are unrestricted."
            );

    [Fact]
    [DddRule("DDD_TA_C004")]
    public void AggregateRootsHavePrivateParameterlessConstructors() =>
        Scenario
            .Given("Aggregate hydration constructors")
            .When(
                DiscoveredRoots.SelectMany(
                    AggregateCreationInspection.ParameterlessConstructor
                )
            )
            .ThenNoViolations(
                "Add a private parameterless constructor to each concrete aggregate root."
            );

    [Fact]
    [DddRule("DDD_TA_C005")]
    public void AggregateRootInstanceConstructorsArePrivate() =>
        Scenario
            .Given("Aggregate construction accessibility")
            .When(
                DiscoveredRoots.SelectMany(
                    AggregateCreationInspection.PrivateConstructors
                )
            )
            .ThenNoViolations(
                "Make all concrete aggregate-root instance constructors private."
            );

    [Fact]
    [DddRule("DDD_TA_C006")]
    public void AggregateRootsExposePublicStaticCreationFactories() =>
        Scenario
            .Given("Aggregate creation factory contracts")
            .When(
                DiscoveredRoots.SelectMany(
                    AggregateCreationInspection.StructuralFactory
                )
            )
            .ThenNoViolations(
                "Expose a public static non-generic creation factory returning the aggregate type, separate from Reconstitute."
            );

    [Fact]
    [DddRule("DDD_TA_P001")]
    public void AggregateIdsAreDeclaredInTheSameFileAsTheirRoots()
    {
        if (ConcreteAggregateRoots.Length == 0)
            return;
        Scenario
            .Given("Identity source placement")
            .When(
                DomainTypeSourceInspection.IdentityInSameFile(
                    AppContext.BaseDirectory,
                    typeof(DddTacticalAggregateTests).Assembly.GetName().Name!,
                    DomainAssembly.GetName().Name!,
                    ConcreteAggregateRoots,
                    AggregateContractInspection.SharedType(
                        ToolkitDomainAssembly,
                        "Entity`1"
                    )
                )
            )
            .ThenNoViolations(
                "Declare each identity type in the same .cs file as its aggregate root or entity."
            );
    }

    private static Type[] DiscoveredRoots =>
        DomainEventContractInspection.AggregateRoots(DomainAssembly, Root);

    private static Type Contract =>
        AggregateContractInspection.SharedType(
            ToolkitDomainAssembly,
            "IAggregateRoot"
        );
    private static Type Root =>
        AggregateContractInspection.SharedType(
            ToolkitDomainAssembly,
            "AggregateRoot`1"
        );
    private static Type EventType =>
        AggregateContractInspection.DomainEventType(ToolkitDomainAssembly);
}
