using Company.BoundedContext.DesignTests.Common;
using static Company.BoundedContext.DesignTests.Common.DomainTestModel;

namespace Company.BoundedContext.DesignTests;

public class DddTacticalRepositoryTests
{
    [Fact]
    [DddRule("DDD_TR_O006")]
    public void EveryConcreteAggregateRootOwnsARepositoryInterface() =>
        Scenario
            .Given("Aggregate repository coverage")
            .When(
                RepositoryInspection.AggregateRepositories(
                    DomainAssembly.GetTypes(),
                    AggregateContractInspection.SharedType(
                        ToolkitDomainAssembly,
                        "AggregateRoot`1"
                    ),
                    TypedRepository
                )
            )
            .ThenNoViolations(
                "Declare a public aggregate-specific interface extending IRepository<TAggregate, TId> in its own file beside each concrete aggregate root."
            );

    /// <summary>
    /// Checks that repository implementations are outside the domain assembly.
    /// This keeps persistence details outside the domain while allowing domain-owned ports.
    /// DDD principle: Repository contracts express domain needs while infrastructure implements persistence.
    /// </summary>
    [Fact]
    [DddRule("DDD_TR_P001")]
    public void RepositoryImplementationsRespectPersistenceIgnoranceOfTheDomain() =>
        Scenario
            .Given("Repository placement")
            .When(
                RepositoryInspection.Repositories(
                    DomainAssembly,
                    [TypedRepository]
                )
            )
            .ThenNoViolations(
                "Move repository implementation classes to an infrastructure assembly. Keep persistence-agnostic repository interfaces in the domain if needed and wire implementations in the composition root."
            );

    /// <summary>
    /// Checks repository interfaces across all production layers belong to the domain assembly.
    /// This keeps aggregate persistence contracts owned by the domain that needs them.
    /// DDD principle: The domain defines repository abstractions; outer layers provide implementations.
    /// </summary>
    [Fact]
    [DddRule("DDD_TR_O001")]
    public void RepositoryContractsAreOwnedByTheDomainModel()
    {
        Scenario
            .Given("Repository interface placement")
            .When(
                RepositoryInspection.RepositoryInterfaces(
                    DomainAssembly,
                    Production,
                    RepositoryInspection.IsRepositoryInterface
                )
            )
            .ThenNoViolations(
                "Move each reported repository interface to the domain assembly beside its aggregate (or shared domain contracts). Keep implementations outside the domain and update project references inward. Repository interfaces may have any name when they inherit IRepository, including generic forms."
            );
    }

    /// <summary>Checks aggregate repository interfaces have their own file beside their aggregate.</summary>
    [Fact]
    [DddRule("DDD_TR_O005")]
    public void AggregateRepositoryInterfacesLiveInSeparateFilesBesideTheirAggregates()
    {
        if (ConcreteAggregateRoots.Length == 0)
            return;

        var repositories = DomainAssembly
            .GetTypes()
            .Where(type =>
                type.IsInterface && type != Marker && type != TypedRepository
            )
            .Where(type => Marker.IsAssignableFrom(type))
            .ToArray();
        var pairs = repositories
            .SelectMany(repository =>
                repository
                    .GetInterfaces()
                    .Where(contract =>
                        contract.IsGenericType
                        && contract.GetGenericTypeDefinition()
                            == TypedRepository
                    )
                    .Select(contract =>
                        (
                            Repository: repository.FullName!,
                            Aggregate: contract.GetGenericArguments()[0]
                        )
                    )
                    .Where(pair =>
                        ConcreteAggregateRoots.Contains(pair.Aggregate)
                    )
                    .Select(pair =>
                        (pair.Repository, Aggregate: pair.Aggregate.FullName!)
                    )
            )
            .Distinct()
            .ToArray();
        Scenario
            .Given("Aggregate repository source placement")
            .When(
                DomainTypeSourceInspection.InspectSources(
                    AppContext.BaseDirectory,
                    typeof(DddTacticalRepositoryTests).Assembly.GetName().Name!,
                    DomainAssembly.GetName().Name!,
                    sources =>
                        DomainTypeSourceInspection
                            .RepositoriesBesideAggregates(pairs, sources)
                            .Concat(
                                repositories
                                    .Where(repository =>
                                        !pairs.Any(pair =>
                                            pair.Repository
                                            == repository.FullName
                                        )
                                    )
                                    .Select(repository =>
                                        $"{repository.FullName}: cannot resolve a corresponding concrete aggregate. Extend IRepository<TAggregate, TId> with the aggregate type to verify source placement."
                                    )
                            ),
                    "aggregate repository"
                )
            )
            .ThenNoViolations(
                "Place each aggregate-specific repository interface in its own source file in the same folder as its corresponding aggregate."
            );
    }

    [Fact]
    [DddRule("DDD_TR_O002")]
    public void ToolkitContainsEmptyRepositoryMarker() =>
        Scenario
            .Given("Repository marker")
            .When(RepositoryContractInspection.Marker(Marker))
            .ThenNoViolations(
                "Declare public interface IRepository { } in DddToolkit.Domain, with no declared or inherited members."
            );

    [Fact]
    [DddRule("DDD_TR_O003")]
    public void ToolkitContainsTypedRepositoryExtendingMarker() =>
        Scenario
            .Given("Typed repository interface")
            .When(
                RepositoryContractInspection.TypedInterface(
                    TypedRepository,
                    Marker
                )
            )
            .ThenNoViolations(
                "Declare public interface IRepository<TAggregate, TId> : IRepository in DddToolkit.Domain."
            );

    [Fact]
    [DddRule("DDD_TR_C001")]
    public void TypedRepositoryConstrainsAggregateUsingItsIdParameter() =>
        Scenario
            .Given("Repository aggregate constraint")
            .When(
                RepositoryContractInspection.AggregateConstraint(
                    TypedRepository,
                    AggregateContractInspection.SharedType(
                        ToolkitDomainAssembly,
                        "AggregateRoot`1"
                    )
                )
            )
            .ThenNoViolations(
                "Constrain the first parameter with where TAggregate : AggregateRoot<TId>, using the second generic parameter as TId."
            );

    [Fact]
    [DddRule("DDD_TR_O004")]
    public void RepositoryContractSourceExistsInToolkit() =>
        Scenario
            .Given("Repository contract source placement")
            .When(
                DomainProjectInspection.ToolkitSources(
                    AppContext.BaseDirectory,
                    typeof(DddTacticalRepositoryTests).Assembly.GetName().Name!,
                    DomainAssembly.GetName().Name!,
                    "IRepository.cs"
                )
            )
            .ThenNoViolations(
                "Place shared repository contract source in lib/DddToolkit.Domain/IRepository.cs. Both interfaces may share that file."
            );

    [Fact]
    [DddRule("DDD_TR_C002")]
    public void TypedRepositoryProvidesAsyncExists() =>
        Scenario
            .Given("Repository Exists operation")
            .When(
                RepositoryContractInspection.Operation(
                    TypedRepository,
                    "Exists"
                )
            )
            .ThenNoViolations(
                "Add public instance Task<bool> Exists(TId id) to IRepository<TAggregate, TId>. Extra parameters, overloads, and methods are allowed."
            );

    [Fact]
    [DddRule("DDD_TR_C003")]
    public void TypedRepositoryProvidesAsyncById() =>
        Scenario
            .Given("Repository ById operation")
            .When(
                RepositoryContractInspection.Operation(TypedRepository, "ById")
            )
            .ThenNoViolations(
                "Add public instance Task<TAggregate> ById(TId id) to IRepository<TAggregate, TId>. Extra parameters, overloads, and methods are allowed."
            );

    [Fact]
    [DddRule("DDD_TR_C004")]
    public void TypedRepositoryProvidesAsyncSave() =>
        Scenario
            .Given("Repository Save operation")
            .When(
                RepositoryContractInspection.Operation(TypedRepository, "Save")
            )
            .ThenNoViolations(
                "Add public instance Task Save(TAggregate aggregate) to IRepository<TAggregate, TId>. Extra parameters, overloads, and methods are allowed."
            );

    [Fact]
    [DddRule("DDD_TR_C005")]
    public void TypedRepositoryProvidesAsyncRemove() =>
        Scenario
            .Given("Repository Remove operation")
            .When(
                RepositoryContractInspection.Operation(
                    TypedRepository,
                    "Remove"
                )
            )
            .ThenNoViolations(
                "Add public instance Task Remove(TAggregate aggregate) to IRepository<TAggregate, TId>. Extra parameters, overloads, and methods are allowed."
            );

    private static Type Marker =>
        AggregateContractInspection.SharedType(
            ToolkitDomainAssembly,
            "IRepository",
            "Declare public interface IRepository { } in DddToolkit.Domain with no declared or inherited members."
        );
    private static Type TypedRepository =>
        AggregateContractInspection.SharedType(
            ToolkitDomainAssembly,
            "IRepository`2"
        );
}
