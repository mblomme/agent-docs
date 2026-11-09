using System.Reflection;
using System.Runtime.CompilerServices;

namespace Company.BoundedContext.DesignTests.Common;

internal static class DomainAssertionInspection
{
    private const BindingFlags PublicMethods =
        BindingFlags.Public
        | BindingFlags.Static
        | BindingFlags.Instance
        | BindingFlags.DeclaredOnly;
    private static readonly string[] GuardNames =
    [
        "EnsureNotNull",
        "EnsureNotNullOrWhiteSpace",
        "EnsureValidId",
    ];
    private static readonly Guid ValidId = new(
        "00000000-0000-0000-0000-000000000001"
    );

    public static IEnumerable<string> ClassShape(Type type)
    {
        if (
            !type.IsClass
            || !type.IsVisible
            || !type.IsAbstract
            || !type.IsSealed
            || type.IsGenericType
        )
            yield return $"{type.Name}: expected public static non-generic class DomainAsserts in DddToolkit.Domain.";
    }

    public static IEnumerable<string> Naming(Type type)
    {
        foreach (var method in Methods(type))
        {
            var name = method.Name;
            if (
                name != "Ensure"
                && !(
                    name.StartsWith("Ensure", StringComparison.Ordinal)
                    && name.Length > 6
                    && char.IsUpper(name[6])
                    && name[6..].All(char.IsLetterOrDigit)
                )
            )
                yield return $"{Describe(method)}: use Ensure followed by a PascalCase description of the assertion, such as EnsureNotNullOrWhiteSpace. The optional Ensure primitive is also allowed.";
        }
    }

    public static IEnumerable<string> ReusableSignatures(
        Type type,
        Assembly domain,
        string sharedNamespace
    )
    {
        foreach (var method in Methods(type))
        {
            var signatures = method
                .GetParameters()
                .Select(p => p.ParameterType)
                .Append(method.ReturnType)
                .Concat(
                    method
                        .GetGenericArguments()
                        .SelectMany(t => t.GetGenericParameterConstraints())
                );
            foreach (
                var referenced in signatures.SelectMany(TypeParts).Distinct()
            )
                if (
                    !referenced.IsGenericParameter
                    && !IsStandardLibrary(referenced)
                    && !(
                        (
                            referenced.Assembly == domain
                            || referenced.Assembly == type.Assembly
                        )
                        && (
                            referenced.Namespace == sharedNamespace
                            || referenced.Namespace?.StartsWith(
                                sharedNamespace + ".",
                                StringComparison.Ordinal
                            ) == true
                        )
                    )
                )
                    yield return $"{Describe(method)}: signature depends on {referenced.FullName}, outside standard-library and shared Domain types. Move aggregate-specific assertions to that aggregate's assertion class; keep DomainAsserts reusable.";
        }
    }

    public static IEnumerable<string> RequiredGuards(Type type, Type idContract)
    {
        foreach (var name in GuardNames)
            if (CompatibleGuards(type, name, idContract).Length == 0)
                yield return MissingGuard(type, name);
    }

    public static IEnumerable<string> Behavior(
        Type type,
        string name,
        Type idContract,
        Type domainException,
        Func<Guid, object>? idFactory = null
    )
    {
        var guards = CompatibleGuards(type, name, idContract);
        if (guards.Length == 0)
        {
            yield return MissingGuard(type, name);
            yield break;
        }
        if (!typeof(Exception).IsAssignableFrom(domainException))
        {
            yield return $"{domainException.Name}: expected the shared DomainException to derive from Exception.";
            yield break;
        }
        var (cases, failure) = Cases(name, idContract, idFactory);
        if (failure is not null)
        {
            yield return failure;
            yield break;
        }
        foreach (var guard in guards)
        foreach (var example in cases)
        {
            var arguments = guard
                .GetParameters()
                .Select(
                    (p, index) =>
                        index == 0 ? example.Value
                        : p.Name?.Contains(
                            "message",
                            StringComparison.OrdinalIgnoreCase
                        ) == true
                            ? $"Value must satisfy {name}."
                        : "value"
                )
                .ToArray();
            var error = Record.Exception(() =>
            {
                guard.Invoke(null, arguments);
            });
            if (
                error is TargetInvocationException { InnerException: { } inner }
            )
                error = inner;
            var member = $"{Describe(guard)} [{example.Name}]";
            if (!example.Rejected && error is not null)
                yield return $"{member}: valid input was rejected with {error.GetType().Name}: {error.Message}. Accept this valid value without throwing.";
            if (example.Rejected)
            {
                if (error is null)
                    yield return $"{member}: invalid input was silently accepted. Throw DomainException or an ArgumentException subtype with a nonblank message.";
                else if (
                    error is not ArgumentException
                    && !domainException.IsInstanceOfType(error)
                )
                    yield return $"{member}: unexpected {error.GetType().Name}; reject invalid input with DomainException or an ArgumentException subtype instead.";
                else if (string.IsNullOrWhiteSpace(error.Message))
                    yield return $"{member}: rejection message is blank. Describe which assertion failed.";
            }
        }
    }

    private static MethodInfo[] CompatibleGuards(
        Type type,
        string name,
        Type idContract
    )
    {
        var result = new List<MethodInfo>();
        foreach (
            var candidate in Methods(type)
                .Where(m => m.Name == name && m.IsStatic)
        )
        {
            var method = candidate;
            if (method.IsGenericMethodDefinition)
            {
                if (
                    name != "EnsureNotNull"
                    || method.GetGenericArguments().Length != 1
                )
                    continue;
                try
                {
                    method = method.MakeGenericMethod(typeof(object));
                }
                catch (ArgumentException)
                {
                    continue;
                }
            }
            if (method.ContainsGenericParameters || !IsSynchronous(method))
                continue;
            var parameters = method.GetParameters();
            var input = name switch
            {
                "EnsureNotNull" => typeof(object),
                "EnsureNotNullOrWhiteSpace" => typeof(string),
                "EnsureValidId" => idContract,
                _ => throw new ArgumentOutOfRangeException(nameof(name)),
            };
            if (
                parameters.Length > 0
                && parameters[0].ParameterType == input
                && parameters
                    .Skip(1)
                    .All(p => p.ParameterType == typeof(string))
            )
                result.Add(method);
        }
        return result.ToArray();
    }

    private static bool IsSynchronous(MethodInfo method)
    {
        var result = method.ReturnType;
        return !method.IsDefined(typeof(AsyncStateMachineAttribute), false)
            && !typeof(Task).IsAssignableFrom(result)
            && result != typeof(ValueTask)
            && !(
                result.IsGenericType
                && result.GetGenericTypeDefinition() == typeof(ValueTask<>)
            )
            && !result.IsByRef
            && !result.IsPointer;
    }

    private static (GuardCase[] Cases, string? Failure) Cases(
        string name,
        Type idContract,
        Func<Guid, object>? factory
    )
    {
        if (name == "EnsureNotNull")
            return (
                [
                    new("non-null object", new object(), false),
                    new("null", null, true),
                ],
                null
            );
        if (name == "EnsureNotNullOrWhiteSpace")
            return (
                [
                    new("meaningful text", " useful value ", false),
                    new("null", null, true),
                    new("empty text", "", true),
                    new("whitespace", " \t\r\n", true),
                ],
                null
            );
        if (factory is null)
            return (
                [],
                "EnsureValidId: supply an identity factory to create valid and empty IEntityId examples from supplied Guids."
            );
        object? valid = null;
        object? empty = null;
        var error = Record.Exception(() =>
        {
            valid = factory(ValidId);
            empty = factory(Guid.Empty);
        });
        if (error is not null)
            return (
                [],
                $"AssertionIdFactory threw {error.GetType().Name}. Configure a factory that constructs valid and empty IDs without validation or new ID generation."
            );
        if (
            valid is null
            || empty is null
            || !idContract.IsInstanceOfType(valid)
            || !idContract.IsInstanceOfType(empty)
        )
            return (
                [],
                $"AssertionIdFactory must return non-null {idContract.Name} values for both supplied Guids. Correct the supplied behavior test example."
            );
        return (
            [
                new("nonempty identity", valid, false),
                new("empty identity", empty, true),
            ],
            null
        );
    }

    private static IEnumerable<MethodInfo> Methods(Type type) =>
        type.GetMethods(PublicMethods).Where(m => !m.IsSpecialName);

    private static string Describe(MethodInfo method) =>
        $"{method.DeclaringType?.Name}.{method.Name}{(method.IsGenericMethod ? "<" + string.Join(", ", method.GetGenericArguments().Select(t => t.Name)) + ">" : "")}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})";

    private static string MissingGuard(Type type, string name) =>
        $"{type.Name}.{name}: no compatible public static synchronous guard found. Add {name} with its guarded value first ({(name == "EnsureNotNull" ? "object or generic T callable with object" : name == "EnsureNotNullOrWhiteSpace" ? "string" : "IEntityId")}), followed only by optional name/message string parameters. Void or value returns are allowed; Ensure(bool, string) does not replace a named guard.";

    private static IEnumerable<Type> TypeParts(Type type)
    {
        if (type.HasElementType)
        {
            foreach (var element in TypeParts(type.GetElementType()!))
                yield return element;
            yield break;
        }
        yield return type;
        if (type.IsGenericType)
            foreach (var argument in type.GetGenericArguments())
            foreach (var part in TypeParts(argument))
                yield return part;
    }

    private static bool IsStandardLibrary(Type type)
    {
        var assembly = type.Assembly.GetName().Name;
        return (
                type.Namespace == "System"
                || type.Namespace?.StartsWith(
                    "System.",
                    StringComparison.Ordinal
                ) == true
            )
            && (
                type.Assembly == typeof(object).Assembly
                || assembly is "System" or "mscorlib" or "netstandard"
                || assembly?.StartsWith("System.", StringComparison.Ordinal)
                    == true
            );
    }

    private sealed record GuardCase(string Name, object? Value, bool Rejected);
}
