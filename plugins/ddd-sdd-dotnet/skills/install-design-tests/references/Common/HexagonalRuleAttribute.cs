namespace Company.BoundedContext.DesignTests.Common;

[AttributeUsage(
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = false
)]
internal sealed class HexagonalRuleAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}
