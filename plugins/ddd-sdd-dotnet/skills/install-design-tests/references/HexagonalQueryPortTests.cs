using Company.BoundedContext.DesignTests.Common;

namespace Company.BoundedContext.DesignTests;

public class HexagonalQueryPortTests
{
    [Fact]
    [HexagonalRule("HEX_QP_O001")]
    public void CommonAndNamedQueryPortsResideInDrivenFolder() =>
        Check("placement");

    [Fact]
    [HexagonalRule("HEX_QP_C001")]
    public void CommonQueryPortsArePublicGenericInterfaces() => Check("shape");

    [Fact]
    [HexagonalRule("HEX_QP_C002")]
    public void FetchIsTheOnlyOperationAndReturnsTaskOfOutput() =>
        Check("fetch");

    [Fact]
    [HexagonalRule("HEX_QP_C003")]
    public void NamedPortsRepresentExactlyOneQuery() => Check("single-query");

    [Fact]
    [HexagonalRule("HEX_QP_O002")]
    public void QueryInputsAreScalarsOrContractsDataObjects() => Check("input");

    [Fact]
    [HexagonalRule("HEX_QP_O003")]
    public void QueryOutputsAreScalarsCollectionsOrContractsDataObjects() =>
        Check("output");

    [Fact]
    [HexagonalRule("HEX_QP_D001")]
    public void WorkflowsInjectQueriesThroughInterfaces() => Check("injection");

    private static void Check(string rule)
    {
        var assembly = AssemblyDiscovery.ApplicationAssembly(
            DomainTestModel.Production
        );
        Scenario
            .Given("Query port " + rule)
            .When(
                QueryPortInspection
                    .Checkout(assembly.GetName().Name!)
                    .Where(v => v.Rule == rule || v.Rule == "analysis")
                    .Select(v => v.Message)
            )
            .ThenNoViolations(
                "Follow query-port contracts, driven-interface injection, and Contracts/Data DTO placement.",
                "Hexagonal"
            );
    }
}
