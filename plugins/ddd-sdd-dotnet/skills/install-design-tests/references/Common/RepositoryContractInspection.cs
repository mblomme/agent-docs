using System.Reflection;

namespace Company.BoundedContext.DesignTests.Common;

internal static class RepositoryContractInspection
{
    private const BindingFlags DeclaredMembers =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    public static IEnumerable<string> Marker(Type type)
    {
        if (!type.IsInterface || !type.IsVisible || type.IsGenericType)
            yield return $"{type.Name}: expected public non-generic interface IRepository in DddToolkit.Domain.";
        if (!type.IsInterface)
            yield break;
        foreach (
            var member in type.GetInterfaces()
                .Append(type)
                .SelectMany(t => t.GetMembers(DeclaredMembers))
        )
            yield return $"{type.Name}: marker must have no declared or inherited members; found {member.DeclaringType?.Name}.{member.Name} ({member.MemberType}). Move operations and state to the typed repository contract.";
    }

    public static IEnumerable<string> TypedInterface(Type type, Type marker)
    {
        if (!IsTypedInterface(type) || !type.IsVisible)
            yield return $"{type.Name}: expected public interface IRepository<TAggregate, TId> with exactly two generic parameters, in aggregate-then-ID order.";
        if (!type.GetInterfaces().Contains(marker))
            yield return $"{type.Name}: must inherit the non-generic {marker.Name} marker interface.";
    }

    public static IEnumerable<string> AggregateConstraint(Type type, Type root)
    {
        if (
            IsTypedInterface(type)
            && root.IsClass
            && root.IsGenericTypeDefinition
            && root.GetGenericArguments().Length == 1
        )
        {
            var parameters = type.GetGenericArguments();
            foreach (
                var constraint in parameters[0].GetGenericParameterConstraints()
            )
                for (
                    var current = constraint;
                    current is not null;
                    current = current.BaseType
                )
                    if (
                        current.IsGenericType
                        && current.GetGenericTypeDefinition() == root
                        && current.GetGenericArguments()[0] == parameters[1]
                    )
                        yield break;
        }
        yield return $"{type.Name}: expected the first generic parameter TAggregate to be constrained to {root.Name} using the second parameter TId, for example where TAggregate : AggregateRoot<TId>. Preserve that ID when using an intermediate base constraint.";
    }

    public static IEnumerable<string> Operation(Type type, string operation)
    {
        if (!IsTypedInterface(type))
        {
            yield return $"{type.Name}.{operation}: cannot inspect the operation; declare IRepository<TAggregate, TId> with exactly two generic parameters first.";
            yield break;
        }
        var parameters = type.GetGenericArguments();
        var input = operation is "Exists" or "ById"
            ? parameters[1]
            : parameters[0];
        var result = operation switch
        {
            "Exists" => typeof(Task<bool>),
            "ById" => typeof(Task<>).MakeGenericType(parameters[0]),
            "Save" or "Remove" => typeof(Task),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
        var methods = type.GetInterfaces()
            .Append(type)
            .SelectMany(t => t.GetMethods(DeclaredMembers));
        if (
            methods.Any(m =>
                m.Name == operation
                && m.IsPublic
                && !m.IsStatic
                && !m.IsGenericMethod
                && m.ReturnType == result
                && m.GetParameters().Any(p => p.ParameterType == input)
            )
        )
            yield break;

        var resultName = operation switch
        {
            "Exists" => "Task<bool>",
            "ById" => "Task<TAggregate>",
            _ => "Task",
        };
        var inputName = operation is "Exists" or "ById"
            ? "TId id"
            : "TAggregate aggregate";
        yield return $"{type.Name}.{operation}: expected public instance {resultName} {operation}({inputName}), allowing extra parameters in any position. At least one non-generic overload must accept the required input and return the specified Task type; ValueTask, static methods, and by-reference inputs do not satisfy the contract.";
    }

    private static bool IsTypedInterface(Type type) =>
        type.IsInterface
        && type.IsGenericTypeDefinition
        && type.GetGenericArguments().Length == 2;
}
