namespace Company.BoundedContext.DesignTests.Common;

internal static class ValueObjectContractInspection
{
    public const string Remediation =
        "Declare public abstract record ValueObject; in DddToolkit.Domain. A public abstract, non-generic, inheritable class also satisfies the marker contract.";

    public static IEnumerable<string> Marker(Type type)
    {
        if (
            !type.IsClass
            || !type.IsVisible
            || !type.IsAbstract
            || type.IsSealed
            || type.IsGenericType
        )
            yield return $"{type.Name}: expected a public, abstract, non-generic, inheritable ValueObject class in DddToolkit.Domain; an abstract record qualifies.";
    }
}
