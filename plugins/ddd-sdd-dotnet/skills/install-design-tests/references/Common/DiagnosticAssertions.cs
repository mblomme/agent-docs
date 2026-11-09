namespace Company.BoundedContext.DesignTests.Common;

internal static class DiagnosticAssertions
{
    public static void AssertNoViolations(
        string rule,
        IEnumerable<string> violations,
        string action,
        string policy = "DDD"
    )
    {
        var details = violations.ToArray();
        Assert.True(
            details.Length == 0,
            $"{policy} policy failed: {rule}\nViolations ({details.Length}):\n"
                + string.Join("\n", details.Select(detail => $"- {detail}"))
                + $"\nAction: {action}\nFix the implementation or correct explicitly documented type configuration; do not disable the rule to make it pass."
        );
    }

    public static void AssertTypeCoverage(
        string rule,
        IEnumerable<Type> expected,
        IEnumerable<Type> configured
    )
    {
        var required = expected.ToArray();
        var actual = configured.ToArray();
        var failures = required
            .Except(actual)
            .Select(t => $"Missing configuration: {t.Name}")
            .Concat(
                actual
                    .Except(required)
                    .Select(t => $"Unexpected configured type: {t.Name}")
            )
            .Concat(
                actual
                    .GroupBy(t => t)
                    .Where(g => g.Count() > 1)
                    .Select(g =>
                        $"Duplicate configuration: {g.Key.Name} ({g.Count()} entries)"
                    )
            );
        if (required.Length == 0)
            failures = failures.Append(
                "Type discovery returned no types; this check would pass without testing the domain."
            );
        AssertNoViolations(
            rule,
            failures,
            "Check discovery and aggregate namespaces. Each entity must belong to exactly one aggregate."
        );
    }

    public static void AssertDependency(
        Action check,
        string rule,
        string relationship,
        string action
    )
    {
        try
        {
            check();
        }
        catch (Xunit.Sdk.XunitException failure)
        {
            AssertNoViolations(
                rule,
                [
                    relationship
                        + "\nDependency evidence:\n"
                        + System.Text.RegularExpressions.Regex.Replace(
                            failure.Message,
                            @"(?:[A-Za-z_][A-Za-z0-9_]*\.)+([A-Za-z_][A-Za-z0-9_]*)",
                            "$1"
                        ),
                ],
                action
            );
        }
    }
}
