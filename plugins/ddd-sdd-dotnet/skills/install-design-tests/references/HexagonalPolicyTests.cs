using System.Reflection;
using Company.BoundedContext.DesignTests.Common;

namespace Company.BoundedContext.DesignTests;

public class HexagonalPolicyTests
{
    [Fact]
    [HexagonalRule("HEX_POL_O001")]
    public void PolicyPortDeclarationResidesInDrivingFolder()
    {
        var assembly = AssemblyDiscovery.ToolkitAssembly(
            DomainTestModel.Production,
            "Application"
        );
        Check(
            "Policy source placement",
            UseCaseContractInspection.DrivingFolder(
                AppContext.BaseDirectory,
                typeof(HexagonalPolicyTests).Assembly.GetName().Name!,
                assembly.GetName().Name!,
                arity: 1,
                interfaceName: "IPolicy"
            )
        );
    }

    [Fact]
    [HexagonalRule("HEX_POL_O002")]
    public void DrivingPortsContainPublicGenericPolicyInterface() =>
        Inspect(PolicyContractInspection.Shape);

    [Fact]
    [HexagonalRule("HEX_POL_C001")]
    public void PolicyEventParameterIsContravariantAndConstrainedToDomainEvent() =>
        Inspect(t =>
            PolicyContractInspection.EventParameter(
                t,
                AggregateContractInspection.SharedType(
                    DomainTestModel.ToolkitDomainAssembly,
                    "IDomainEvent"
                )
            )
        );

    [Fact]
    [HexagonalRule("HEX_POL_C002")]
    public void PolicyExposesExecuteAcceptingItsEvent() =>
        Inspect(PolicyContractInspection.Execute);

    [Fact]
    [HexagonalRule("HEX_POL_N001")]
    public void PolicyExecuteReturnsTaskWithoutAsyncSuffix() =>
        Inspect(PolicyContractInspection.AsyncExecute);

    [Fact]
    [HexagonalRule("HEX_POL_O003")]
    public void ConcretePoliciesHaveTheirOwnFileInPoliciesFolder() =>
        InspectWorkflow("policy-file");

    [Fact]
    [HexagonalRule("HEX_POL_C003")]
    public void ConcretePoliciesImplementPolicyPort() =>
        InspectWorkflow("policy-interface");

    private static void InspectWorkflow(string rule) =>
        Check(
            "Policy workflow " + rule,
            ApplicationWorkflowInspection
                .Checkout(ApplicationAssembly().GetName().Name!)
                .Where(v => v.Rule == rule || v.Rule == "analysis")
                .Select(v => v.Message),
            "Implement IPolicy<TDomainEvent> and declare each concrete policy in its own Policies/<ClassName>.cs file."
        );

    private static void Inspect(Func<Type, IEnumerable<string>> inspect)
    {
        var assembly = AssemblyDiscovery.ToolkitAssembly(
            DomainTestModel.Production,
            "Application"
        );
        Check(
            "Policy port contract",
            PolicyContractInspection.Contracts(
                assembly.GetTypes(),
                assembly.GetName().Name! + ".Contracts.Driving",
                inspect
            )
        );
    }

    private static Assembly ApplicationAssembly() =>
        AssemblyDiscovery.ApplicationAssembly(DomainTestModel.Production);

    private static void Check(
        string rule,
        IEnumerable<string> violations,
        string action =
            "Declare public IPolicy<in TDomainEvent> where TDomainEvent : IDomainEvent with Task Execute(TDomainEvent domainEvent) directly in Application/Contracts/Driving. Extra parameters, members, overloads, and inherited Execute are allowed."
    ) =>
        Scenario
            .Given(rule)
            .When(violations)
            .ThenNoViolations(action, "Hexagonal");
}
