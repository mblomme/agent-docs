using System.Reflection;

namespace Company.BoundedContext.DesignTests.Common;

internal static class PolicyContractInspection
{
    public static IEnumerable<string> Contracts(
        IEnumerable<Type> types,
        string drivingNamespace,
        Func<Type, IEnumerable<string>> inspect
    )
    {
        var name = drivingNamespace + ".IPolicy`1";
        var candidates = types.Where(t => t.FullName == name).ToArray();
        if (candidates.Length != 1)
        {
            yield return $"{name}: expected exactly one policy port; found {candidates.Length}. Declare public IPolicy<in TDomainEvent> in Application.Contracts.Driving.";
            yield break;
        }
        foreach (var violation in inspect(candidates[0]))
            yield return violation;
    }

    public static IEnumerable<string> Shape(Type type)
    {
        if (!ValidShape(type))
            yield return $"{type}: declare a public, top-level generic interface IPolicy<in TDomainEvent> with exactly one type parameter.";
    }

    public static IEnumerable<string> EventParameter(
        Type type,
        Type eventContract
    )
    {
        if (!ValidShape(type))
        {
            foreach (var violation in Shape(type))
                yield return violation;
            yield break;
        }
        var parameter = type.GetGenericArguments()[0];
        if (
            (
                parameter.GenericParameterAttributes
                & GenericParameterAttributes.VarianceMask
            ) != GenericParameterAttributes.Contravariant
        )
            yield return $"{type}: declare the event parameter contravariant using in TDomainEvent.";
        if (
            !parameter
                .GetGenericParameterConstraints()
                .Any(c => eventContract.IsAssignableFrom(c))
        )
            yield return $"{type}: constrain the event parameter with where TDomainEvent : IDomainEvent from DddToolkit.Domain, or a constraint implying that contract.";
    }

    public static IEnumerable<string> Execute(Type type)
    {
        foreach (var violation in Shape(type))
            yield return violation;
        if (!ValidShape(type))
            yield break;
        if (!InputMethods(type).Any())
            yield return $"{type}: add public instance nongeneric Execute(TDomainEvent domainEvent), accepting the event by value; extra parameters are allowed.";
    }

    public static IEnumerable<string> AsyncExecute(Type type)
    {
        foreach (var violation in Shape(type))
            yield return violation;
        if (!ValidShape(type))
            yield break;
        if (!InputMethods(type).Any(m => m.ReturnType == typeof(Task)))
            yield return $"{type}: add Task Execute(TDomainEvent domainEvent); the return type must be exactly Task.";
        if (Methods(type).Any(m => m.Name == "ExecuteAsync"))
            yield return $"{type}: rename ExecuteAsync to Execute; the Async suffix is forbidden on policy execution methods.";
    }

    private static bool ValidShape(Type type) =>
        type.IsInterface
        && type.IsVisible
        && !type.IsNested
        && type.IsGenericTypeDefinition
        && type.GetGenericArguments().Length == 1;

    private static IEnumerable<MethodInfo> Methods(Type type) =>
        type.GetInterfaces()
            .Append(type)
            .SelectMany(t =>
                t.GetMethods(
                    BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.Instance
                        | BindingFlags.Static
                        | BindingFlags.DeclaredOnly
                )
            );

    private static IEnumerable<MethodInfo> InputMethods(Type type) =>
        Methods(type)
            .Where(m =>
                m.Name == "Execute"
                && m.IsPublic
                && !m.IsStatic
                && !m.IsGenericMethod
                && m.GetParameters()
                    .Any(p => p.ParameterType == type.GetGenericArguments()[0])
            );
}
