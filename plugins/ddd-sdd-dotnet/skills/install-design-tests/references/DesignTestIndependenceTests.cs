using Company.BoundedContext.DesignTests.Common;

namespace Company.BoundedContext.DesignTests;

public class DesignTestIndependenceTests
{
    [Fact]
    public void DesignTestsHaveNoCompiledDependenciesOnProductionTypes()
    {
        var tests = typeof(DesignTestIndependenceTests).Assembly;
        using var stream = File.OpenRead(tests.Location);
        Scenario
            .Given("Reusable design tests")
            .When(
                ProductionDependencyInspection.References(
                    stream,
                    AssemblyDiscovery
                        .ProductionAssemblies(tests)
                        .Select(a => a.GetName().Name!)
                )
            )
            .ThenNoViolations(
                "Remove production type imports, construction, casts, constraints, attributes, and typeof expressions. Use discovered Type values and self-contained regression fixtures."
            );
    }
}
