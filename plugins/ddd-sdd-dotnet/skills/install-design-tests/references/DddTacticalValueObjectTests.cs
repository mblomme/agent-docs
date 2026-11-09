using Company.BoundedContext.DesignTests.Common;
using static Company.BoundedContext.DesignTests.Common.DomainTestModel;

namespace Company.BoundedContext.DesignTests;

public class DddTacticalValueObjectTests
{
    [Fact]
    [DddRule("DDD_TV_P002")]
    public void ConcreteValueObjectsNeverResideInSharedFolder() =>
        Scenario
            .Given("Concrete value object folder placement")
            .When(
                DomainTypeSourceInspection.InspectSources(
                    AppContext.BaseDirectory,
                    typeof(DddTacticalValueObjectTests)
                        .Assembly.GetName()
                        .Name!,
                    DomainAssembly.GetName().Name!,
                    sources =>
                        DomainTypeSourceInspection.ConcreteValueObjectsOutsideShared(
                            ConcreteValueObjects.Select(type => type.FullName!),
                            sources
                        ),
                    "concrete value object"
                )
            )
            .ThenNoViolations(
                "Carefully read the domain-layer specification again for correct folder locations"
            );

    /// <summary>Each source file contains at most one concrete value object.</summary>
    [Fact]
    [DddRule("DDD_TV_P001")]
    public void ConcreteValueObjectsAreDeclaredInSeparateFiles() =>
        Scenario
            .Given("Concrete value object source files")
            .When(
                DomainTypeSourceInspection.OnePerFile(
                    AppContext.BaseDirectory,
                    typeof(DddTacticalValueObjectTests)
                        .Assembly.GetName()
                        .Name!,
                    DomainAssembly.GetName().Name!,
                    ConcreteValueObjects,
                    "value object"
                )
            )
            .ThenNoViolations(
                "Move each concrete value object into a separate .cs file. Abstract base types do not count."
            );

    /// <summary>
    /// Checks that the domain exposes a shared marker base for value objects.
    /// The marker may be an abstract class or record and must support inheritance.
    /// </summary>
    [Fact]
    [DddRule("DDD_TV_O001")]
    public void ToolkitContainsPublicAbstractValueObjectMarker() =>
        Scenario
            .Given("Shared value object marker")
            .When(
                ValueObjectContractInspection.Marker(
                    AggregateContractInspection.SharedType(
                        ToolkitDomainAssembly,
                        "ValueObject",
                        ValueObjectContractInspection.Remediation
                    )
                )
            )
            .ThenNoViolations(ValueObjectContractInspection.Remediation);

    /// <summary>
    /// Checks value objects for mutable nested state and equality overrides.
    /// This guards against values changing unexpectedly or using reference equality.
    /// DDD principle: Value objects are immutable and defined by their values.
    /// </summary>
    [Fact]
    [DddRule("DDD_TV_I001")]
    public void ValueObjectsAreImmutableAndDefineValueEquality()
    {
        Scenario
            .Given("Value object immutability and equality")
            .When(DomainModelInspection.ValueObjects(ConcreteValueObjects))
            .ThenNoViolations(
                "Remove mutation paths, use readonly backing fields and recursively immutable state, and implement value-based Equals/GetHashCode (for example with records). For an unrecognized immutable framework type, review and explicitly extend the immutable-type policy; a read-only interface alone is insufficient."
            );
    }
}
