namespace Company.BoundedContext.DesignTests;

using Company.BoundedContext.DesignTests.Common;
using static Company.BoundedContext.DesignTests.Common.DomainTestModel;

public class DddGeneralPrinciplesTests
{
    [Fact]
    [DddRule("DDD_GP_O003")]
    public void ToolkitContainsDomainExceptionExtendingException()
    {
        var domainException = AggregateContractInspection.SharedType(
            ToolkitDomainAssembly,
            "DomainException",
            "Declare public class DomainException : Exception in DddToolkit.Domain."
        );

        Scenario
            .Given("Shared domain exception")
            .When(
                DomainProjectInspection
                    .ToolkitSources(
                        AppContext.BaseDirectory,
                        typeof(DddGeneralPrinciplesTests)
                            .Assembly.GetName()
                            .Name!,
                        DomainAssembly.GetName().Name!,
                        "DomainException.cs"
                    )
                    .Concat(
                        DomainEventContractInspection.DomainException(
                            domainException
                        )
                    )
            )
            .ThenNoViolations(
                "Place public, concrete, non-generic DomainException : Exception in lib/DddToolkit.Domain/DomainException.cs."
            );
    }
}
