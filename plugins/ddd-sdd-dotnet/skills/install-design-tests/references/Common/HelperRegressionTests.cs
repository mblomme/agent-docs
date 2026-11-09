using static Company.BoundedContext.DesignTests.Common.RegressionFixtures;

namespace Company.BoundedContext.DesignTests.Common;

public class HelperRegressionTests
{
    /// <summary>
    /// Checks constructors and delegated commands that record events pass while queries are excluded.
    /// This ensures event recording through private helpers is recognized without requiring events for reads.
    /// DDD principle: Aggregates record domain facts for state changes, not queries or reconstitution.
    /// </summary>
    [Fact]
    public void AggregateDomainEventsAcceptConstructorsAndDelegatedCommands()
    {
        var sink = typeof(EventfulAggregate).GetMethod(
            "RecordEvent",
            System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic
        )!;
        Assert.Empty(
            AggregateEventInspection.MissingDomainEvents(
                [typeof(EventfulAggregate)],
                [sink],
                method => method.Name == "Query"
            )
        );
    }

    /// <summary>
    /// Checks silent commands and constructors are reported without including queries or reconstitution.
    /// This gives agents concrete event creation guidance and handles recursive helpers safely.
    /// DDD principle: Aggregate state transitions should produce domain facts for future consumers.
    /// </summary>
    [Fact]
    public void AggregateDomainEventsRejectSilentCommandsAndConstructors()
    {
        var violations = AggregateEventInspection
            .MissingDomainEvents(
                [typeof(SilentAggregate)],
                [],
                method => method.Name == "Query"
            )
            .ToArray();
        Assert.Equal(3, violations.Length);
        Assert.Contains(
            violations,
            message => message.Contains(".ctor", StringComparison.Ordinal)
        );
        Assert.Contains(
            violations,
            message => message.Contains("Change()", StringComparison.Ordinal)
        );
        Assert.All(
            violations,
            message =>
            {
                Assert.Contains(
                    "even when it is not described in the specification",
                    message,
                    StringComparison.Ordinal
                );
                Assert.Contains(
                    "future extensibility",
                    message,
                    StringComparison.Ordinal
                );
                Assert.DoesNotContain(
                    "Reconstitute",
                    message,
                    StringComparison.Ordinal
                );
                Assert.DoesNotContain(
                    "Query",
                    message,
                    StringComparison.Ordinal
                );
            }
        );
    }

    /// <summary>
    /// Checks arbitrary interface names inheriting generic and non-generic repository markers.
    /// This prevents aggregate-specific names from escaping repository placement checks.
    /// DDD principle: Repository roles come from domain contracts, not naming prefixes or suffixes.
    /// </summary>
    [Fact]
    public void RepositoryContractsAreIdentifiedByAbstractionRatherThanInterfaceNaming()
    {
        Assert.True(
            RepositoryInspection.IsRepositoryInterface(
                typeof(SomeAggregateName)
            )
        );
        Assert.True(
            RepositoryInspection.IsRepositoryInterface(
                typeof(AnotherAggregateName)
            )
        );
        Assert.True(
            RepositoryInspection.IsRepositoryInterface(
                typeof(IndirectAggregateName)
            )
        );
        Assert.False(
            RepositoryInspection.IsRepositoryInterface(typeof(OtherRoot))
        );
    }

    /// <summary>
    /// Checks that repository interfaces in an outer assembly are reported with their actual and expected layers.
    /// This ensures interface placement failures give agents a concrete move to make.
    /// DDD principle: Repository abstractions belong to the domain rather than technical layers.
    /// </summary>
    [Fact]
    public void DomainOwnershipOfRepositoryContractsRejectsInterfacesInOuterLayers()
    {
        var domain = typeof(object).Assembly;
        var failure = Assert.Single(
            RepositoryInspection.RepositoryInterfaces(
                domain,
                [typeof(HelperRegressionTests).Assembly],
                type => type == typeof(IRepositoryPort<>)
            )
        );
        Assert.Contains(
            typeof(IRepositoryPort<>).Name,
            failure,
            StringComparison.Ordinal
        );
        Assert.Contains(
            typeof(HelperRegressionTests).Assembly.GetName().Name!,
            failure,
            StringComparison.Ordinal
        );
        Assert.Contains(
            domain.GetName().Name!,
            failure,
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// Checks that a repository interface in the configured domain assembly is accepted.
    /// This prevents the placement rule from rejecting legitimate domain-owned contracts.
    /// DDD principle: Domain-owned repository interfaces express persistence needs without implementing persistence.
    /// </summary>
    [Fact]
    public void DomainOwnershipOfRepositoryContractsAllowsInterfacesInTheDomain() =>
        Assert.Empty(
            RepositoryInspection.RepositoryInterfaces(
                typeof(HelperRegressionTests).Assembly,
                [typeof(HelperRegressionTests).Assembly],
                type => type == typeof(IRepositoryPort<>)
            )
        );

    /// <summary>
    /// Checks that failure messages retain every violation and a suggested fix.
    /// This gives agents enough detail to act without losing findings to collection previews.
    /// DDD principle: Explicit domain rules help preserve the model as it evolves.
    /// </summary>
    [Fact]
    public void DddPolicyDiagnosticsReportEveryViolationAndRemediation()
    {
        var violations = Enumerable
            .Range(1, 12)
            .Select(i => $"Example.Type.Member{i}: mutable state")
            .ToArray();
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Encapsulation",
                violations,
                "Make the fields private."
            )
        );
        Assert.Contains(
            "DDD policy failed: Encapsulation",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Action: Make the fields private.",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.All(
            violations,
            violation =>
                Assert.Contains(
                    violation,
                    failure.Message,
                    StringComparison.Ordinal
                )
        );
    }

    /// <summary>
    /// Checks that configuration failures name missing, unexpected and duplicate types.
    /// This helps agents repair test coverage without weakening the domain rules.
    /// DDD principle: Explicit aggregate ownership makes consistency boundaries enforceable.
    /// </summary>
    [Fact]
    public void ExplicitDomainModelCoverageReportsMissingUnexpectedAndDuplicateTypes()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertTypeCoverage(
                "Aggregate membership",
                [typeof(Child)],
                [typeof(OtherRoot), typeof(OtherRoot)]
            )
        );
        Assert.Contains(
            $"Missing configuration: {typeof(Child).Name}",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            $"Unexpected configured type: {typeof(OtherRoot).Name}",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            $"Duplicate configuration: {typeof(OtherRoot).Name}",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// Checks the encapsulation rule against deliberately exposed and protected state.
    /// This verifies that the reusable rule rejects violations and accepts valid entities.
    /// DDD principle: Entities expose behavior that protects their invariants.
    /// </summary>
    [Fact]
    public void EntityEncapsulationRejectsPublicMutationAndAllowsControlledState()
    {
        Assert.Equal(
            2,
            DomainModelInspection.Encapsulation([typeof(MutableEntity)]).Count()
        );
        Assert.Empty(
            DomainModelInspection.Encapsulation([typeof(EncapsulatedEntity)])
        );
    }

    /// <summary>
    /// Checks that a readonly reference to a mutable collection is rejected.
    /// This prevents shallow immutability checks from accepting mutable value objects.
    /// DDD principle: Value object immutability extends to nested state.
    /// </summary>
    [Fact]
    public void ValueObjectImmutabilityRejectsMutableNestedState() =>
        Assert.NotEmpty(
            DomainModelInspection.ValueObjects([typeof(NestedMutableValue)])
        );

    /// <summary>
    /// Checks that a value object without equality overrides is rejected.
    /// This ensures value objects must explicitly provide value-based equality.
    /// DDD principle: Value object equality depends on values rather than object identity.
    /// </summary>
    [Fact]
    public void ValueObjectValueEqualityRejectsReferenceEquality() =>
        Assert.NotEmpty(
            DomainModelInspection.ValueObjects([typeof(ReferenceValue)])
        );

    /// <summary>
    /// Checks that distinct values incorrectly comparing equal are rejected.
    /// This verifies equality behavior rather than merely requiring an override.
    /// DDD principle: All meaningful value components participate in value object equality.
    /// </summary>
    [Fact]
    public void ValueObjectValueEqualityDistinguishesDifferentValues() =>
        Assert.NotEmpty(
            DomainModelInspection.Equality(
                new(
                    () => new BrokenEquality(1),
                    () => new BrokenEquality(1),
                    () => new BrokenEquality(2)
                )
            )
        );

    /// <summary>
    /// Checks that a repository implementation inside the inspected assembly is detected.
    /// This verifies that domain-owned interfaces cannot hide persistence implementations.
    /// DDD principle: The domain remains independent of persistence implementation details.
    /// </summary>
    [Fact]
    public void DomainPersistenceIgnoranceRejectsRepositoryImplementationsInTheDomain() =>
        Assert.Single(
            RepositoryInspection.Repositories(
                typeof(HelperRegressionTests).Assembly,
                [typeof(IRepositoryPort<>)]
            )
        );

    /// <summary>
    /// Checks that calling a foreign child entity is rejected with ownership evidence.
    /// This verifies boundary analysis covers behavior as well as type declarations.
    /// DDD principle: Changes to aggregate internals are controlled by the owning root.
    /// </summary>
    [Fact]
    public void AggregateRootEncapsulationRejectsDirectAccessToForeignChildEntities()
    {
        AggregateDefinition[] aggregates =
        [
            new(typeof(InvadingRoot), []),
            new(typeof(OtherRoot), [typeof(Child)]),
        ];
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DomainModelInspection.AggregateBoundaries(
                [typeof(HelperRegressionTests).Assembly],
                aggregates
            )
        );
        Assert.Contains(
            "DDD policy failed: Aggregate boundaries",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            typeof(Child).Name,
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Dependency evidence:",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains("Action:", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that a collection of foreign aggregate roots is rejected with useful diagnostics.
    /// This ensures generic navigation cannot bypass the identity-reference policy.
    /// DDD principle: Aggregate references use identities rather than live object navigation.
    /// </summary>
    [Fact]
    public void AggregateIdentityReferencesRejectObjectNavigationThroughCollections()
    {
        AggregateDefinition[] aggregates =
        [
            new(typeof(NavigatingRoot), []),
            new(typeof(OtherRoot), []),
        ];
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DomainModelInspection.IdentityReferences(
                [typeof(HelperRegressionTests).Assembly],
                aggregates
            )
        );
        Assert.Contains(
            "DDD policy failed: Cross-aggregate references",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            typeof(OtherRoot).Name,
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Dependency evidence:",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains("Action:", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks that storing a foreign aggregate identity is accepted.
    /// This guards against rejecting the supported way to reference another aggregate.
    /// DDD principle: Identity references preserve separation between aggregates.
    /// </summary>
    [Fact]
    public void AggregateIdentityReferencesAllowReferencesByIdentity() =>
        DomainModelInspection.IdentityReferences(
            [typeof(HelperRegressionTests).Assembly],
            [new(typeof(IdentityRoot), []), new(typeof(OtherRoot), [])]
        );
}
