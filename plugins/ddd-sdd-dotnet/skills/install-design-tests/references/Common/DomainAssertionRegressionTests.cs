using Company.BoundedContext.DesignTests.Common.AssertionFixtures.Shared;

namespace Company.BoundedContext.DesignTests.Common;

public class DomainAssertionRegressionTests
{
    [Theory]
    [InlineData(typeof(ValidGuards))]
    [InlineData(typeof(NamedGuardsOnly))]
    public void ReusableNamedGuardContractsAreAcceptedWithoutRequiringBooleanPrimitive(
        Type assertions
    )
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(assertions);
        // Act
        var violations = DomainAssertionInspection
            .ClassShape(assertions)
            .Concat(DomainAssertionInspection.Naming(assertions))
            .Concat(
                DomainAssertionInspection.ReusableSignatures(
                    assertions,
                    assertions.Assembly,
                    typeof(ValidGuards).Namespace!
                )
            )
            .Concat(
                DomainAssertionInspection.RequiredGuards(
                    assertions,
                    typeof(IFixtureId)
                )
            )
            .ToArray();
        // Assert
        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("EnsureNotNull")]
    [InlineData("EnsureNotNullOrWhiteSpace")]
    [InlineData("EnsureValidId")]
    public void AllCompatibleOverloadsAcceptValidInputsAndRejectInvalidInputs(
        string guard
    )
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(guard);
        // Act
        var violations = Behavior(typeof(ValidGuards), guard);
        // Assert
        Assert.Empty(violations);
    }

    [Theory]
    [InlineData(typeof(ShapeFixtures.InstanceAssertions))]
    [InlineData(typeof(ShapeFixtures.AbstractAssertions))]
    [InlineData(typeof(ShapeFixtures.IAssertions))]
    [InlineData(typeof(ShapeFixtures.StructAssertions))]
    [InlineData(typeof(ShapeFixtures.GenericAssertions<>))]
    public void InvalidAssertionClassShapesAreRejected(Type assertions)
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(assertions);
        // Act
        var violation = Assert.Single(
            DomainAssertionInspection.ClassShape(assertions)
        );
        // Assert
        Assert.Contains(
            "public static non-generic class DomainAsserts",
            violation,
            StringComparison.Ordinal
        );
        Assert.NotEmpty(
            DomainAssertionInspection.ClassShape(ShapeFixtures.HiddenType)
        );
    }

    [Fact]
    public void NamingReportsEveryBadNameAndAllowsTheOptionalEnsureName()
    {
        // Arrange
        var assertions = typeof(WrongNames);
        // Act
        var violations = DomainAssertionInspection.Naming(assertions).ToArray();
        // Assert
        Assert.Equal(3, violations.Length);
        Assert.Contains(
            violations,
            message => message.Contains("Meaningful", StringComparison.Ordinal)
        );
        Assert.Contains(
            violations,
            message =>
                message.Contains("Ensurelowercase", StringComparison.Ordinal)
        );
        Assert.Contains(
            violations,
            message =>
                message.Contains("Ensure_BadName", StringComparison.Ordinal)
        );
        Assert.DoesNotContain(
            violations,
            message => message.Contains(".Ensure()", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void AggregateSpecificParametersReturnsNestedArgumentsAndConstraintsAreRejected()
    {
        // Arrange
        var assertions = typeof(AggregateSpecificGuards);
        // Act
        var violations = DomainAssertionInspection
            .ReusableSignatures(
                assertions,
                assertions.Assembly,
                typeof(ValidGuards).Namespace!
            )
            .ToArray();
        // Assert
        Assert.Equal(4, violations.Length);
        foreach (
            var method in new[]
            {
                "EnsureReturn",
                "EnsureDirect",
                "EnsureNested",
                "EnsureConstrained",
            }
        )
            Assert.Contains(
                violations,
                message =>
                    message.Contains(method, StringComparison.Ordinal)
                    && message.Contains(
                        "AggregateSpecificValue",
                        StringComparison.Ordinal
                    )
            );
    }

    [Fact]
    public void MissingGuardsReportTheFullRequiredCatalogue()
    {
        // Arrange
        var assertions = typeof(NoGuards);
        // Act
        var violations = DomainAssertionInspection
            .RequiredGuards(assertions, typeof(IFixtureId))
            .ToArray();
        // Assert
        Assert.Equal(3, violations.Length);
        Assert.All(
            violations,
            message =>
                Assert.Contains(
                    "no compatible public static synchronous guard",
                    message,
                    StringComparison.Ordinal
                )
        );
    }

    [Theory]
    [InlineData(typeof(AsyncTextGuard), "EnsureNotNullOrWhiteSpace")]
    [InlineData(typeof(RestrictedGenericNullGuard), "EnsureNotNull")]
    [InlineData(typeof(InstanceNullGuard), "EnsureNotNull")]
    [InlineData(typeof(WrongInputNullGuard), "EnsureNotNull")]
    [InlineData(typeof(WrongTrailingNullGuard), "EnsureNotNull")]
    public void IncompatibleSignaturesFailWithoutInvocationErrors(
        Type assertions,
        string guard
    )
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(assertions);
        ArgumentNullException.ThrowIfNull(guard);
        // Act
        var violations = Behavior(assertions, guard);
        // Assert
        Assert.Contains(
            "no compatible public static synchronous guard",
            Assert.Single(violations),
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(typeof(NoOpNullGuard), "EnsureNotNull", "silently accepted")]
    [InlineData(
        typeof(InvertedNullGuard),
        "EnsureNotNull",
        "valid input was rejected"
    )]
    [InlineData(
        typeof(WrongExceptionTextGuard),
        "EnsureNotNullOrWhiteSpace",
        "unexpected InvalidOperationException"
    )]
    [InlineData(
        typeof(BlankMessageTextGuard),
        "EnsureNotNullOrWhiteSpace",
        "rejection message is blank"
    )]
    [InlineData(
        typeof(BrokenOverloadTextGuard),
        "EnsureNotNullOrWhiteSpace",
        "silently accepted"
    )]
    public void BrokenGuardBehaviorProducesActionableDiagnostics(
        Type assertions,
        string guard,
        string evidence
    )
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(assertions);
        ArgumentNullException.ThrowIfNull(guard);
        ArgumentNullException.ThrowIfNull(evidence);
        // Act
        var violations = Behavior(assertions, guard);
        // Assert
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Guard behavior",
                violations,
                "Correct the named guard's acceptance and rejection behavior."
            )
        );
        Assert.Contains(
            assertions.Name + "." + guard,
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(evidence, failure.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Action: Correct the named guard",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void InvalidIdFactoriesProduceConfigurationGuidance()
    {
        // Arrange
        Func<Guid, object>[] factories =
        [
            _ => new object(),
            _ =>
                throw new InvalidOperationException(
                    "Cannot construct identity."
                ),
        ];
        // Act
        var failures = factories
            .SelectMany(factory =>
                DomainAssertionInspection.Behavior(
                    typeof(ValidGuards),
                    "EnsureValidId",
                    typeof(IFixtureId),
                    typeof(FixtureDomainException),
                    factory
                )
            )
            .ToArray();
        var missing = DomainAssertionInspection
            .Behavior(
                typeof(ValidGuards),
                "EnsureValidId",
                typeof(IFixtureId),
                typeof(FixtureDomainException)
            )
            .ToArray();
        // Assert
        Assert.Equal(2, failures.Length);
        Assert.All(
            failures,
            message =>
                Assert.Contains(
                    "AssertionIdFactory",
                    message,
                    StringComparison.Ordinal
                )
        );
        Assert.Contains(
            "supply an identity factory",
            Assert.Single(missing),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void MissingAssertionUtilityGivesPlacementAndDeclarationGuidance()
    {
        // Arrange
        var assembly = typeof(DomainAssertionRegressionTests).Assembly;
        // Act
        Action act = () =>
            AggregateContractInspection.SharedType(
                assembly,
                "DomainAsserts",
                "Declare public static class DomainAsserts in DddToolkit.Domain."
            );
        // Assert
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(act);
        Assert.Contains(
            "DomainAsserts",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains("found 0", failure.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Action: Declare public static class DomainAsserts",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    private static string[] Behavior(Type assertions, string guard) =>
        DomainAssertionInspection
            .Behavior(
                assertions,
                guard,
                typeof(IFixtureId),
                typeof(FixtureDomainException),
                value => new FixtureId(value)
            )
            .ToArray();
}
