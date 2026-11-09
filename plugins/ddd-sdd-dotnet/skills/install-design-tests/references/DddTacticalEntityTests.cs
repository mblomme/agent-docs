using Company.BoundedContext.DesignTests.Common;
using static Company.BoundedContext.DesignTests.Common.DomainTestModel;

namespace Company.BoundedContext.DesignTests;

public class DddTacticalEntityTests
{
    /// <summary>
    /// Checks that entities expose no public mutable fields or public setters.
    /// This prevents callers from bypassing domain methods when changing state.
    /// DDD principle: Entities protect their invariants through controlled state changes.
    /// </summary>
    [Fact]
    [DddRule("DDD_TE_E001")]
    public void EntityEncapsulationProtectsInvariantsByRestrictingPublicMutation() =>
        Scenario
            .Given("Encapsulation")
            .When(DomainModelInspection.Encapsulation(Entities))
            .ThenNoViolations(
                "Make mutable fields private and public setters private or protected. Expose named domain methods that enforce invariants."
            );

    /// <summary>Shared entity IDs have a discoverable, public factory class.</summary>
    [Fact]
    [DddRule("DDD_TE_O001")]
    public void ToolkitContainsPublicEntityIdFactoryClass() =>
        Scenario
            .Given("Entity ID factory class")
            .When(EntityContractInspection.IdFactoryClass(IdFactory))
            .ThenNoViolations(
                "Declare public class EntityId or public static class EntityId in DddToolkit.Domain."
            );

    /// <summary>The factory creates the requested ID type from a supplied Guid.</summary>
    [Fact]
    [DddRule("DDD_TE_C001")]
    public void EntityIdFactoryCreatesItsGenericIdTypeFromGuid() =>
        Scenario
            .Given("Entity ID factory method")
            .When(EntityContractInspection.IdFactoryMethod(IdFactory))
            .ThenNoViolations(
                "Add public static TId Create<TId>(Guid value) to EntityId. Generic constraints and construction mechanics are not prescribed."
            );

    /// <summary>Entities share an abstract base with one ID parameter.</summary>
    [Fact]
    [DddRule("DDD_TE_C002")]
    public void ToolkitContainsAbstractEntityBaseWithOneIdParameter() =>
        Scenario
            .Given("Entity base class")
            .When(EntityContractInspection.BaseClass(EntityBase))
            .ThenNoViolations(
                "Declare public abstract class Entity<TId> in DddToolkit.Domain with exactly one generic ID parameter."
            );

    /// <summary>The entity base declares equality with its own entity type.</summary>
    [Fact]
    [DddRule("DDD_TE_Q001")]
    public void EntityBaseImplementsEquatableForItsOwnEntityType() =>
        Scenario
            .Given("Entity equality contract")
            .When(EntityContractInspection.Equatable(EntityBase))
            .ThenNoViolations(
                "Make Entity<TId> implement IEquatable<Entity<TId>>, preserving its own TId parameter."
            );

    /// <summary>Concrete non-root entities provide a private hydration constructor.</summary>
    [Fact]
    [DddRule("DDD_TE_C003")]
    public void ConcreteChildEntitiesHavePrivateParameterlessConstructors()
    {
        var roots = DomainEventContractInspection.AggregateRoots(
            DomainAssembly,
            AggregateContractInspection.SharedType(
                ToolkitDomainAssembly,
                "AggregateRoot`1"
            )
        );
        Scenario
            .Given("Concrete entity hydration constructors")
            .When(
                EntityContractInspection
                    .EntityTypes(DomainAssembly, EntityBase)
                    .Where(type => !type.IsAbstract && !roots.Contains(type))
                    .SelectMany(EntityContractInspection.HydrationConstructor)
            )
            .ThenNoViolations(
                "Add a private parameterless constructor to each concrete non-root entity. Aggregate roots are covered by DDD_TA_C004."
            );
    }

    /// <summary>Abstract entity bases provide protected hydration constructors.</summary>
    [Fact]
    [DddRule("DDD_TE_C004")]
    public void AbstractEntityBasesHaveProtectedParameterlessConstructors() =>
        Scenario
            .Given("Abstract entity hydration constructors")
            .When(
                EntityContractInspection
                    .EntityTypes(DomainAssembly, EntityBase)
                    .Where(type => type.IsAbstract)
                    .SelectMany(EntityContractInspection.HydrationConstructor)
            )
            .ThenNoViolations(
                "Add a protected parameterless constructor to every abstract entity base, including Entity<TId> and abstract aggregate-root bases."
            );

    [Fact]
    [DddRule("DDD_TE_P001")]
    public void ChildEntityIdsAreDeclaredInTheSameFileAsTheirEntities()
    {
        var children = ConcreteEntities
            .Except(ConcreteAggregateRoots)
            .ToArray();
        if (children.Length == 0)
            return;
        Scenario
            .Given("Identity source placement")
            .When(
                DomainTypeSourceInspection.IdentityInSameFile(
                    AppContext.BaseDirectory,
                    typeof(DddTacticalEntityTests).Assembly.GetName().Name!,
                    DomainAssembly.GetName().Name!,
                    children,
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

    private static Type IdFactory =>
        AggregateContractInspection.SharedType(
            ToolkitDomainAssembly,
            "EntityId"
        );
    private static Type EntityBase =>
        AggregateContractInspection.SharedType(
            ToolkitDomainAssembly,
            "Entity`1"
        );
}
