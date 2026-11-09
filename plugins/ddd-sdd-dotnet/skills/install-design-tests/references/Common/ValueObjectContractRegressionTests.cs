namespace Company.BoundedContext.DesignTests.Common;

public class ValueObjectContractRegressionTests
{
    [Theory]
    [InlineData(typeof(ClassMarker))]
    [InlineData(typeof(RecordMarker))]
    public void AbstractClassAndRecordMarkersAreAccepted(Type marker)
    {
        ArgumentNullException.ThrowIfNull(marker);
        Assert.Empty(ValueObjectContractInspection.Marker(marker));
    }

    [Theory]
    [InlineData(typeof(IInterfaceMarker))]
    [InlineData(typeof(StructMarker))]
    [InlineData(typeof(ConcreteMarker))]
    [InlineData(typeof(StaticMarker))]
    [InlineData(typeof(GenericMarker<>))]
    [InlineData(typeof(GenericMarker<int>))]
    [InlineData(typeof(HiddenMarker))]
    public void InvalidMarkersReportTheProblemAndSuggestedDeclaration(
        Type marker
    )
    {
        ArgumentNullException.ThrowIfNull(marker);
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Shared value object marker",
                ValueObjectContractInspection.Marker(marker),
                ValueObjectContractInspection.Remediation
            )
        );
        Assert.Contains(marker.Name, failure.Message, StringComparison.Ordinal);
        Assert.Contains(
            "public, abstract, non-generic, inheritable",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Action: Declare public abstract record ValueObject; in DddToolkit.Domain",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void MissingMarkerReportsTheExpectedDeclarationAndPlacement()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            AggregateContractInspection.SharedType(
                typeof(ValueObjectContractRegressionTests).Assembly,
                "ValueObject",
                ValueObjectContractInspection.Remediation
            )
        );
        Assert.Contains(
            "ValueObject",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains("found 0", failure.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Action: Declare public abstract record ValueObject; in DddToolkit.Domain",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    // These nested fixtures must be publicly visible to exercise Type.IsVisible.
#pragma warning disable CA1034, CA1515
    public abstract class ClassMarker
    {
        // A marker is allowed to have members; this rule does not require emptiness.
        public abstract string Description { get; }
    }

    public abstract record RecordMarker;

    public interface IInterfaceMarker;

    public readonly record struct StructMarker;

    public class ConcreteMarker;

    public static class StaticMarker;

    public abstract class GenericMarker<T>;

    private abstract class HiddenMarker;
#pragma warning restore CA1034, CA1515
}
