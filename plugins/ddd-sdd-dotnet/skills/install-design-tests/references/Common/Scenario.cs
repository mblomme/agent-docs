namespace Company.BoundedContext.DesignTests.Common;

/// <summary>
/// Reads a design rule as a scenario: given a subject, when it is inspected,
/// then the inspection should report no violations.
/// </summary>
internal static class Scenario
{
    public static Inspection Given(string subject) => new(subject);

    internal sealed class Inspection(string subject)
    {
        public Result When(IEnumerable<string> violations) =>
            new(subject, violations);

        public ExecutableResult When(Action inspection) =>
            new(subject, inspection);
    }

    internal sealed class ExecutableResult(string subject, Action inspection)
    {
        public void ThenCompletesWithoutViolations()
        {
            try
            {
                inspection();
            }
            catch (Xunit.Sdk.XunitException failure)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Given {subject}, the inspection reported a violation: {failure.Message}"
                );
            }
        }
    }

    internal sealed class Result(string subject, IEnumerable<string> violations)
    {
        public void ThenNoViolations(string action, string policy = "DDD") =>
            DiagnosticAssertions.AssertNoViolations(
                subject,
                violations,
                action,
                policy
            );
    }
}
