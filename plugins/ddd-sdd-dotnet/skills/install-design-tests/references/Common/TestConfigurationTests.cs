using static Company.BoundedContext.DesignTests.Common.DomainTestModel;

namespace Company.BoundedContext.DesignTests.Common;

public class TestConfigurationTests
{
    /// <summary>
    /// Checks that every discovered entity belongs to exactly one aggregate.
    /// This keeps boundary checks complete as the domain grows.
    /// DDD principle: An entity belongs to one aggregate consistency boundary.
    /// </summary>
    [Fact]
    public void AggregateConsistencyBoundariesAssignEachEntityToExactlyOneAggregate()
    {
        if (ConcreteEntities.Length == 0)
            return;

        var assigned = Aggregates
            .SelectMany(a => a.Members.Append(a.Root))
            .ToArray();
        DiagnosticAssertions.AssertTypeCoverage(
            "Aggregate membership",
            Entities,
            assigned
        );
    }
}
