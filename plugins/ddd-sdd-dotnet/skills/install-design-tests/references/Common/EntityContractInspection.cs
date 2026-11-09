using System.Reflection;

namespace Company.BoundedContext.DesignTests.Common;

internal static class EntityContractInspection
{
    public static IEnumerable<string> IdFactoryClass(Type type)
    {
        if (!type.IsClass || !type.IsVisible || type.IsGenericType)
            yield return $"{type.Name}: expected a public, non-generic EntityId class in DddToolkit.Domain; a static class is allowed.";
    }

    public static IEnumerable<string> IdFactoryMethod(Type type)
    {
        if (
            !type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(method =>
                    method.Name == "Create"
                    && method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == 1
                    && method.ReturnType == method.GetGenericArguments()[0]
                    && method
                        .GetParameters()
                        .Select(parameter => parameter.ParameterType)
                        .SequenceEqual([typeof(Guid)])
                )
        )
            yield return $"{type.Name}.Create<TId>(Guid value): expected a public static factory with exactly one generic parameter, returning that same TId. Instance methods, different input types, and other return types do not satisfy the contract.";
    }

    public static IEnumerable<string> BaseClass(Type type)
    {
        if (
            !type.IsClass
            || !type.IsVisible
            || !type.IsAbstract
            || type.IsSealed
            || !type.IsGenericTypeDefinition
            || type.GetGenericArguments().Length != 1
        )
            yield return $"{type.Name}: expected public abstract class Entity<TId> in DddToolkit.Domain with exactly one generic ID parameter; it must be inheritable.";
    }

    public static IEnumerable<string> Equatable(Type type)
    {
        if (
            type.IsGenericTypeDefinition
            && type.GetGenericArguments().Length == 1
            && type.GetInterfaces()
                .Any(contract =>
                    contract.IsGenericType
                    && contract.GetGenericTypeDefinition()
                        == typeof(IEquatable<>)
                    && IsSelfType(contract.GetGenericArguments()[0], type)
                )
        )
            yield break;

        yield return $"{type.Name}: expected IEquatable<Entity<TId>> using this entity base class and its own ID parameter. IEquatable<TId>, IEquatable<object>, or an entity using a different ID type is insufficient.";
    }

    public static Type[] EntityTypes(Assembly assembly, Type entityBase) =>
        assembly
            .GetTypes()
            .Where(type => type.IsClass && IsEntityType(type, entityBase))
            .ToArray();

    private static bool IsEntityType(Type type, Type entityBase)
    {
        for (
            Type? current = type;
            current is not null;
            current = current.BaseType
        )
            if (
                current == entityBase
                || current.IsGenericType
                    && current.GetGenericTypeDefinition() == entityBase
            )
                return true;
        return false;
    }

    public static IEnumerable<string> HydrationConstructor(Type entity)
    {
        const BindingFlags flags =
            BindingFlags.Instance
            | BindingFlags.Public
            | BindingFlags.NonPublic;
        var constructor = entity
            .GetConstructors(flags)
            .SingleOrDefault(candidate =>
                candidate.GetParameters().Length == 0
            );
        var accessibility = entity.IsAbstract ? "protected" : "private";
        if (
            constructor is null
            || !(
                entity.IsAbstract ? constructor.IsFamily : constructor.IsPrivate
            )
        )
            yield return $"{entity.FullName}: declare a {accessibility} parameterless constructor for persistence hydration. A constructor with optional parameters is not parameterless; inherited constructors do not satisfy this requirement.";
    }

    private static bool IsSelfType(Type candidate, Type entity) =>
        candidate.IsGenericType
        && candidate.GetGenericTypeDefinition() == entity
        && candidate
            .GetGenericArguments()
            .SequenceEqual(entity.GetGenericArguments());
}
