namespace Company.BoundedContext.DesignTests.Common;

public class TimeZoneValuePolicyTests
{
    [Fact]
    public void SuppliedTimeZoneValuesAreRecognizedAsImmutable() =>
        Assert.Empty(
            DomainModelInspection.ValueObjects([new ZoneValue().GetType()])
        );

    [Fact]
    public void RecognizingTimeZonesDoesNotAllowMutableContainers() =>
        Assert.NotEmpty(
            DomainModelInspection.ValueObjects([new ZoneArray().GetType()])
        );

    private sealed record ZoneValue
    {
        public TimeZoneInfo Zone { get; } = TimeZoneInfo.Utc;
    }

    private sealed record ZoneArray
    {
        public TimeZoneInfo[] Zones { get; } = [TimeZoneInfo.Utc];
    }
}
