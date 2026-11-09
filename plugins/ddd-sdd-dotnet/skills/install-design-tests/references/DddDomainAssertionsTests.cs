using Company.BoundedContext.DesignTests.Common;
using static Company.BoundedContext.DesignTests.Common.DomainTestModel;

namespace Company.BoundedContext.DesignTests;

public class DddDomainAssertionsTests
{
    [Fact]
    [DddRule("DDD_GP_O001")]
    public void ToolkitContainsPublicStaticDomainAsserts() =>
        Scenario
            .Given("Shared assertion utility")
            .When(DomainAssertionInspection.ClassShape(Assertions))
            .ThenNoViolations(
                "Declare public static class DomainAsserts in DddToolkit.Domain."
            );

    [Fact]
    [DddRule("DDD_GP_O002")]
    public void DomainAssertsSourceExistsInToolkit() =>
        Scenario
            .Given("Shared assertion source placement")
            .When(
                DomainProjectInspection.ToolkitSources(
                    AppContext.BaseDirectory,
                    typeof(DddDomainAssertionsTests).Assembly.GetName().Name!,
                    DomainAssembly.GetName().Name!,
                    "DomainAsserts.cs"
                )
            )
            .ThenNoViolations(
                "Place the assertion utility in lib/DddToolkit.Domain/DomainAsserts.cs."
            );

    [Fact]
    [DddRule("DDD_GP_N001")]
    public void SharedAssertionMethodsHaveEnsurePrefixedNames() =>
        Scenario
            .Given("Assertion method naming")
            .When(DomainAssertionInspection.Naming(Assertions))
            .ThenNoViolations(
                "Rename public assertion methods with descriptive Ensure-prefixed names. Ensure(bool, string) may remain as an optional primitive."
            );

    [Fact]
    [DddRule("DDD_GP_R001")]
    public void SharedAssertionSignaturesRemainReusableAcrossAggregates() =>
        Scenario
            .Given("Reusable assertion signatures")
            .When(
                DomainAssertionInspection.ReusableSignatures(
                    Assertions,
                    DomainAssembly,
                    Assertions.Namespace!
                )
            )
            .ThenNoViolations(
                "Use standard-library types, generic parameters, and shared Domain abstractions. Move aggregate-specific guards into the aggregate's own assertion class."
            );

    [Fact]
    [DddRule("DDD_GP_A001")]
    public void SharedAssertionsProvideTheRequiredNamedGuards() =>
        Scenario
            .Given("Required assertion guards")
            .When(
                DomainAssertionInspection.RequiredGuards(Assertions, IdContract)
            )
            .ThenNoViolations(
                "Add EnsureNotNull, EnsureNotNullOrWhiteSpace, and EnsureValidId with the documented reusable input signatures. Do not replace these guards with a boolean condition helper."
            );

    private static Type Assertions =>
        AggregateContractInspection.SharedType(
            ToolkitDomainAssembly,
            "DomainAsserts",
            "Declare public static class DomainAsserts in DddToolkit.Domain."
        );
    private static Type IdContract =>
        AggregateContractInspection.SharedType(
            ToolkitDomainAssembly,
            "IEntityId"
        );
}
