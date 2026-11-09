using System.Reflection;

namespace Company.BoundedContext.DesignTests.Common;

internal sealed record AggregateCreationExample(
    Type Root,
    string MethodName,
    Func<object?[]> Arguments
);

internal static class AggregateCreationInspection
{
    private const BindingFlags Constructors =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static IEnumerable<string> ParameterlessConstructor(Type root)
    {
        if (
            !root.GetConstructors(Constructors)
                .Any(c => c.IsPrivate && c.GetParameters().Length == 0)
        )
            yield return $"{root.Name}: add a private parameterless constructor for persistence hydration.";
    }

    public static IEnumerable<string> PrivateConstructors(Type root) =>
        root.GetConstructors(Constructors)
            .Where(c => !c.IsPrivate)
            .Select(c =>
                $"{root.Name}.{c}: make this instance constructor private; expose creation through a public static factory."
            );

    public static IEnumerable<string> StructuralFactory(Type root)
    {
        if (
            !root.GetMethods(
                    BindingFlags.Public
                        | BindingFlags.Static
                        | BindingFlags.DeclaredOnly
                )
                .Any(m =>
                    !m.IsSpecialName
                    && !m.IsGenericMethod
                    && m.Name != "Reconstitute"
                    && m.ReturnType == root
                )
        )
            yield return $"{root.FullName}: declare a public static non-generic creation factory returning exactly {root.Name}; Reconstitute is not a creation factory.";
    }

    public static IEnumerable<string> FactoryContract(
        AggregateCreationExample example
    )
    {
        if (Factories(example).Length == 0)
            yield return $"{example.Root.Name}.{example.MethodName}: declare a public static non-generic factory returning exactly {example.Root.Name}; supply valid arguments in the behavior test example.";
    }

    public static IEnumerable<string> FreshInstances(
        AggregateCreationExample example
    )
    {
        var methods = Factories(example);
        if (methods.Length == 0)
        {
            foreach (var violation in FactoryContract(example))
                yield return violation;
            yield break;
        }
        object? first = null;
        object? second = null;
        var failure = Record.Exception(() =>
        {
            var arguments = example.Arguments();
            // Reuse identical valid arguments so changing inputs cannot hide caching.
            first = example.Root.InvokeMember(
                example.MethodName,
                BindingFlags.Public
                    | BindingFlags.Static
                    | BindingFlags.InvokeMethod
                    | BindingFlags.DeclaredOnly,
                null,
                null,
                arguments,
                System.Globalization.CultureInfo.InvariantCulture
            );
            second = example.Root.InvokeMember(
                example.MethodName,
                BindingFlags.Public
                    | BindingFlags.Static
                    | BindingFlags.InvokeMethod
                    | BindingFlags.DeclaredOnly,
                null,
                null,
                arguments,
                System.Globalization.CultureInfo.InvariantCulture
            );
        });
        if (failure is not null)
            yield return $"{example.Root.Name}.{example.MethodName}: factory invocation failed ({failure.GetBaseException().GetType().Name}: {failure.GetBaseException().Message}). Correct the factory or its configured valid arguments.";
        else if (
            first is null
            || second is null
            || first.GetType() != example.Root
            || second.GetType() != example.Root
            || ReferenceEquals(first, second)
        )
            yield return $"{example.Root.Name}.{example.MethodName}: each invocation must return a non-null new instance of {example.Root.Name}; do not return a cached instance.";
    }

    private static MethodInfo[] Factories(AggregateCreationExample example) =>
        example
            .Root.GetMethods(
                BindingFlags.Public
                    | BindingFlags.Static
                    | BindingFlags.DeclaredOnly
            )
            .Where(m =>
                m.Name == example.MethodName
                && !m.IsGenericMethod
                && m.ReturnType == example.Root
            )
            .ToArray();
}
