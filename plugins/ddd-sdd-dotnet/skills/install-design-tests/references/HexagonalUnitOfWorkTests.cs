using Company.BoundedContext.DesignTests.Common;

namespace Company.BoundedContext.DesignTests;

public class HexagonalUnitOfWorkTests
{
    [Fact]
    [HexagonalRule("HEX_UOW_O001")]
    public void UnitOfWorkPortDeclarationResidesInDrivenFolder()
    {
        var assembly = AssemblyDiscovery.ToolkitAssembly(
            DomainTestModel.Production,
            "Application"
        );
        Check(
            "Unit-of-work port source placement",
            UnitOfWorkContractInspection.DrivenFolder(
                AppContext.BaseDirectory,
                typeof(HexagonalUnitOfWorkTests).Assembly.GetName().Name!,
                assembly.GetName().Name!
            ),
            "Declare public interface IUnitOfWork directly in Contracts/Driven with the matching namespace."
        );
    }

    [Fact]
    [HexagonalRule("HEX_UOW_O002")]
    public void DrivenPortsContainPublicNongenericUnitOfWorkInterface() =>
        Inspect(UnitOfWorkContractInspection.Shape);

    [Fact]
    [HexagonalRule("HEX_UOW_C001")]
    public void UnitOfWorkExposesAsyncDo() =>
        Inspect(UnitOfWorkContractInspection.Do);

    [Fact]
    [HexagonalRule("HEX_UOW_C002")]
    public void UnitOfWorkExposesAsyncSaveForRepositoryAndAggregate() =>
        Inspect(t =>
            UnitOfWorkContractInspection.Save(
                t,
                DomainContract("IAggregateRoot"),
                DomainContract("IRepository")
            )
        );

    [Fact]
    [HexagonalRule("HEX_UOW_C003")]
    public void UnitOfWorkExposesTypedRepositoryAccess() =>
        Inspect(t =>
            UnitOfWorkContractInspection.Repo(t, DomainContract("IRepository"))
        );

    private static void Inspect(Func<Type, IEnumerable<string>> inspect)
    {
        var assembly = AssemblyDiscovery.ToolkitAssembly(
            DomainTestModel.Production,
            "Application"
        );
        Check(
            "Unit-of-work port contract",
            UnitOfWorkContractInspection.Contracts(
                assembly.GetTypes(),
                assembly.GetName().Name! + ".Contracts.Driven",
                inspect
            ),
            "Declare public IUnitOfWork with Task Do(), Task Save<TRepository>(IAggregateRoot aggregate), and TRepository Repo<TRepository>() in Application.Contracts.Driven. Constrain both repository parameters with where TRepository : IRepository, using DddToolkit.Domain contracts."
        );
    }

    private static Type DomainContract(string name) =>
        AggregateContractInspection.SharedType(
            DomainTestModel.ToolkitDomainAssembly,
            name
        );

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
