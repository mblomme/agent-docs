using System.Reflection;

namespace Company.BoundedContext.DesignTests.Common;

internal static class AggregateContractInspection
{
    private const BindingFlags Members =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static;

    public static Type SharedType(
        Assembly domain,
        string name,
        string? action = null
    )
    {
        var candidates = domain
            .GetTypes()
            .Where(t => t.Namespace == "DddToolkit.Domain" && t.Name == name)
            .ToArray();
        DiagnosticAssertions.AssertNoViolations(
            "Toolkit domain contract discovery",
            candidates.Length == 1
                ? []
                :
                [
                    $"{name}: expected exactly one type in the DddToolkit.Domain namespace; found {candidates.Length}.",
                ],
            action
                ?? $"Declare {name.Split('`')[0]} in the DddToolkit.Domain namespace."
        );
        return candidates[0];
    }

    public static Type DomainEventType(Assembly domain)
    {
        var candidates = domain
            .GetTypes()
            .Where(t =>
                t.FullName == "DddToolkit.Domain.IDomainEvent" && t.IsInterface
            )
            .ToArray();
        DiagnosticAssertions.AssertNoViolations(
            "Domain event contract discovery",
            candidates.Length == 1
                ? []
                :
                [
                    $"IDomainEvent: expected exactly one domain event interface in DddToolkit.Domain; found {candidates.Length}.",
                ],
            "Declare one IDomainEvent interface in DddToolkit.Domain."
        );
        return candidates[0];
    }

    public static IEnumerable<string> Interface(Type type)
    {
        if (!type.IsInterface || !type.IsVisible || type.IsGenericType)
            yield return $"{type.Name}: expected a public, non-generic IAggregateRoot interface.";
    }

    public static IEnumerable<string> BaseClass(Type type, Type contract)
    {
        if (!type.IsClass || !type.IsAbstract || !type.IsVisible)
            yield return $"{type.Name}: expected a public abstract aggregate root base class.";
        if (!type.GetInterfaces().Contains(contract))
            yield return $"{type.Name}: must implement {contract.Name}.";
    }

    public static IEnumerable<string> EntityInheritance(Type root, Type entity)
    {
        if (
            root.IsGenericTypeDefinition
            && root.GetGenericArguments().Length == 1
            && entity.IsClass
            && entity.IsGenericTypeDefinition
            && entity.GetGenericArguments().Length == 1
        )
        {
            var id = root.GetGenericArguments()[0];
            for (
                var current = root.BaseType;
                current is not null;
                current = current.BaseType
            )
                if (
                    current.IsGenericType
                    && current.GetGenericTypeDefinition() == entity
                    && current.GetGenericArguments()[0] == id
                )
                    yield break;
        }

        yield return $"{root.Name}: expected inheritance from {entity.Name} using the aggregate root's own ID type parameter. An aggregate root is a special type of entity; exposing an Id property alone is insufficient.";
    }

    public static IEnumerable<string> Identity(Type type)
    {
        var parameters = type.IsGenericTypeDefinition
            ? type.GetGenericArguments()
            : [];
        if (parameters.Length != 1)
        {
            yield return $"{type.Name}: expected exactly one generic ID parameter, as AggregateRoot<TId>.";
            yield break;
        }
        var properties = type.GetProperties(Members)
            .Where(p => p.Name == "Id")
            .ToArray();
        if (
            !properties.Any(p =>
                p.PropertyType == parameters[0]
                && p.GetMethod is { IsPublic: true, IsStatic: false }
                && p.GetIndexParameters().Length == 0
            )
        )
            yield return $"{type.Name}.Id: expected a public instance getter using the root's generic ID parameter; an inherited property is allowed.";
    }

    public static IEnumerable<string> Version(Type type)
    {
        if (
            !ContractTypes(type)
                .SelectMany(t => t.GetProperties(Members))
                .Any(p =>
                    p.Name == "ConcurrencyVersion"
                    && p.PropertyType == typeof(long)
                    && p.GetMethod is { IsPublic: true, IsStatic: false }
                    && p.SetMethod is null
                    && p.GetIndexParameters().Length == 0
                )
        )
            yield return $"{type.Name}.ConcurrencyVersion: expected getter-only instance property long ConcurrencyVersion {{ get; }}.";
    }

    public static IEnumerable<string> Events(Type type, Type eventType)
    {
        if (
            !Methods(type)
                .Any(m =>
                    m.Name == "DomainEvents"
                    && PublicQuery(m)
                    && IsReadOnlyEvents(m.ReturnType, eventType)
                )
        )
            yield return $"{type.Name}.DomainEvents(): expected a public parameterless instance method returning IReadOnlyCollection<{eventType.Name}> or IReadOnlyList<{eventType.Name}>; a property or mutable collection does not satisfy this contract.";
    }

    public static IEnumerable<string> Clear(Type type)
    {
        if (
            !Methods(type)
                .Any(m =>
                    m.Name == "ClearDomainEvents"
                    && PublicQuery(m)
                    && m.ReturnType == typeof(void)
                )
        )
            yield return $"{type.Name}.ClearDomainEvents(): expected public instance void ClearDomainEvents() with no parameters.";
    }

    public static IEnumerable<string> Raise(Type type, Type eventType)
    {
        if (
            !type.GetMethods(Members)
                .Any(m =>
                    m.Name == "RaiseDomainEvent"
                    && m.IsFamily
                    && !m.IsStatic
                    && !m.IsGenericMethod
                    && m.ReturnType == typeof(void)
                    && m.GetParameters()
                        .Select(p => p.ParameterType)
                        .SequenceEqual([eventType])
                )
        )
            yield return $"{type.Name}.RaiseDomainEvent: expected protected instance void RaiseDomainEvent({eventType.Name} fact).";
    }

    private static IEnumerable<Type> ContractTypes(Type type) =>
        type.IsInterface ? type.GetInterfaces().Append(type) : [type];

    private static IEnumerable<MethodInfo> Methods(Type type) =>
        ContractTypes(type).SelectMany(t => t.GetMethods(Members));

    private static bool PublicQuery(MethodInfo method) =>
        method.IsPublic
        && !method.IsStatic
        && !method.IsGenericMethod
        && method.GetParameters().Length == 0;

    private static bool IsReadOnlyEvents(Type type, Type eventType) =>
        type.IsGenericType
        && (
            type.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)
            || type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
        )
        && type.GetGenericArguments()[0] == eventType;
}
