using System.Reflection;
using Company.BoundedContext.DesignTests.Common;

namespace Company.BoundedContext.DesignTests;

public class HexagonalUseCaseTests
{
    [Fact]
    [HexagonalRule("HEX_UC_O001")]
    public void UseCasePortDeclarationsResideInDrivingFolder()
    {
        var assembly = AssemblyDiscovery.ToolkitAssembly(
            DomainTestModel.Production,
            "Application"
        );
        Check(
            "Use-case port source placement",
            UseCaseContractInspection.DrivingFolder(
                AppContext.BaseDirectory,
                typeof(HexagonalUseCaseTests).Assembly.GetName().Name!,
                assembly.GetName().Name!
            ),
            "Declare both use case interfaces directly in Contracts/Driving with the matching namespace."
        );
    }

    [Fact]
    [HexagonalRule("HEX_UC_O002")]
    public void DrivingPortsContainBothPublicGenericUseCaseInterfaces() =>
        Inspect(UseCaseContractInspection.Shape);

    [Fact]
    [HexagonalRule("HEX_UC_C001")]
    public void BothUseCasePortsExposeExecuteAcceptingTheirInput() =>
        Inspect(UseCaseContractInspection.Execute);

    [Fact]
    [HexagonalRule("HEX_UC_N001")]
    public void ExecuteReturnsTaskWithoutAsyncSuffix() =>
        Inspect(UseCaseContractInspection.AsyncExecute);

    [Fact]
    [HexagonalRule("HEX_UC_O004")]
    public void QueryUseCasePortDeclarationResidesInDrivingFolder()
    {
        var assembly = AssemblyDiscovery.ToolkitAssembly(
            DomainTestModel.Production,
            "Application"
        );
        Check(
            "Query use-case port source placement",
            UseCaseContractInspection.DrivingFolder(
                AppContext.BaseDirectory,
                typeof(HexagonalUseCaseTests).Assembly.GetName().Name!,
                assembly.GetName().Name!,
                arity: 2,
                interfaceName: "IQueryUseCase"
            ),
            "Declare the query use case interface directly in Contracts/Driving with the matching namespace."
        );
    }

    [Fact]
    [HexagonalRule("HEX_UC_O003")]
    public void DrivingPortsContainPublicGenericQueryUseCaseInterface() =>
        InspectQuery(UseCaseContractInspection.Shape);

    [Fact]
    [HexagonalRule("HEX_UC_C002")]
    public void QueryUseCasePortExposesExecuteAcceptingItsInput() =>
        InspectQuery(UseCaseContractInspection.Execute);

    [Fact]
    [HexagonalRule("HEX_UC_N002")]
    public void QueryExecuteReturnsTaskOfOutputWithoutAsyncSuffix() =>
        InspectQuery(UseCaseContractInspection.AsyncExecute);

    [Fact]
    [HexagonalRule("HEX_UC_D001")]
    public void ConcreteUseCasesDoNotInjectRepositories() =>
        InspectImplementation("injection");

    [Fact]
    [HexagonalRule("HEX_UC_D002")]
    public void EachExecutionSavesAtMostOneAggregateType() =>
        InspectImplementation("save");

    [Fact]
    [HexagonalRule("HEX_UC_D003")]
    public void SuccessfulExecutionsCompleteTheirFinalCommit() =>
        InspectImplementation("commit");

    [Fact]
    [HexagonalRule("HEX_UC_D004")]
    public void QueriesDoNotDependOnUnitOfWork() =>
        InspectWorkflow("query-unit-of-work");

    [Fact]
    [HexagonalRule("HEX_UC_D005")]
    public void QueriesDoNotDependOnDomainLayer() =>
        InspectWorkflow("query-domain");

    [Fact]
    [HexagonalRule("HEX_UC_O005")]
    public void ConcreteCommandsHaveTheirOwnFileAtApplicationRoot() =>
        InspectWorkflow("command-file");

    [Fact]
    [HexagonalRule("HEX_UC_C003")]
    public void ConcreteCommandsImplementUseCasePort() =>
        InspectWorkflow("command-interface");

    [Fact]
    [HexagonalRule("HEX_UC_O006")]
    public void CommandInputsAreDeclaredInTheirWorkflowFile() =>
        InspectWorkflow("command-input");

    [Fact]
    [HexagonalRule("HEX_UC_O007")]
    public void ConcreteQueriesHaveTheirOwnFileInQueriesFolder() =>
        InspectWorkflow("query-file");

    [Fact]
    [HexagonalRule("HEX_UC_C004")]
    public void ConcreteQueriesImplementQueryUseCasePort() =>
        InspectWorkflow("query-interface");

    [Fact]
    [HexagonalRule("HEX_UC_O008")]
    public void QueryInputsAreDeclaredInTheirWorkflowFile() =>
        InspectWorkflow("query-input");

    [Fact]
    [HexagonalRule("HEX_UC_O009")]
    public void CustomQueryResultsAreDeclaredInContractsData() =>
        InspectWorkflow("query-output");

    private static void InspectWorkflow(string rule) =>
        Check(
            "Application workflow " + rule,
            ApplicationWorkflowInspection
                .Checkout(ApplicationAssembly().GetName().Name!)
                .Where(v => v.Rule == rule || v.Rule == "analysis")
                .Select(v => v.Message),
            "Follow the workflow interface, file placement, input, output, and read-only query conventions."
        );

    private static void InspectImplementation(string rule) =>
        Check(
            "Use-case persistence " + rule,
            UseCaseImplementationInspection
                .Checkout(ApplicationAssembly().GetName().Name!)
                .Where(v => v.Rule == rule || v.Rule == "analysis")
                .Select(v => v.Message),
            "Access repositories through the unit of work, save one aggregate type per execution, and complete Do before returning success."
        );

    private static void InspectQuery(Func<Type, IEnumerable<string>> inspect) =>
        Inspect(inspect, arity: 2, interfaceName: "IQueryUseCase");

    private static void Inspect(
        Func<Type, IEnumerable<string>> inspect,
        int? arity = null,
        string interfaceName = "IUseCase"
    )
    {
        var assembly = AssemblyDiscovery.ToolkitAssembly(
            DomainTestModel.Production,
            "Application"
        );
        Check(
            "Use-case port contract",
            UseCaseContractInspection.Contracts(
                assembly.GetTypes(),
                assembly.GetName().Name! + ".Contracts.Driving",
                inspect,
                arity,
                interfaceName
            ),
            interfaceName == "IQueryUseCase"
                ? "Declare public IQueryUseCase<TInput, TOutput> with Task<TOutput> Execute(TInput input) in Application.Contracts.Driving. Name the method Execute, without Async."
                : "Declare public IUseCase<TInput> with Task Execute(TInput input), and public IUseCase<TInput, TOutput> with Task<TOutput> Execute(TInput input), in Application.Contracts.Driving. Name the method Execute, without Async."
        );
    }

    private static Assembly ApplicationAssembly() =>
        AssemblyDiscovery.ApplicationAssembly(DomainTestModel.Production);

    private static void Check(
        string rule,
        IEnumerable<string> violations,
        string action
    ) =>
        Scenario
            .Given(rule)
            .When(violations)
            .ThenNoViolations(action, "Hexagonal");
}
