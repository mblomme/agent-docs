using System.Reflection;
using System.Runtime.CompilerServices;

namespace Company.BoundedContext.DesignTests.Common;

internal static class AggregateAssertionInspection
{
    public static IEnumerable<string> Classes(
        Assembly domain,
        IEnumerable<Type> roots
    )
    {
        var aggregates = roots.ToArray();
        if (aggregates.Length == 0)
            yield return "No concrete aggregate roots were discovered; assertion conventions cannot be verified.";
        foreach (var root in aggregates)
            if (Candidates(domain, root).Length != 1)
                yield return $"{root.Name}: expected exactly one non-generic class {root.Namespace}.{root.Name}Asserts. Declare the aggregate-specific assertion class; it may initially be empty.";
    }

    public static IEnumerable<string> Naming(
        Assembly domain,
        IEnumerable<Type> roots
    ) => roots.SelectMany(root => Candidates(domain, root)).SelectMany(Naming);

    public static IEnumerable<string> Naming(Type assertions)
    {
        const BindingFlags flags =
            BindingFlags.DeclaredOnly
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.Instance;
        foreach (
            var method in assertions
                .GetMethods(flags)
                .Where(method =>
                    (
                        method.IsPublic
                        || method.IsAssembly
                        || method.IsFamilyOrAssembly
                    )
                    && !method.IsSpecialName
                    && !method.IsDefined(
                        typeof(CompilerGeneratedAttribute),
                        false
                    )
                )
        )
        {
            var name = method.Name;
            if (
                !(
                    name.StartsWith("Ensure", StringComparison.Ordinal)
                    && name.Length > 6
                    && char.IsUpper(name[6])
                    && name[6..].All(char.IsLetterOrDigit)
                )
            )
                yield return $"{assertions.FullName}.{method}: name assertion methods Ensure followed by a PascalCase description, such as EnsureTaskCanBeCompleted; bare Ensure is not descriptive.";
        }
    }

    private static Type[] Candidates(Assembly domain, Type root) =>
        domain
            .GetTypes()
            .Where(type =>
                type.IsClass
                && !type.IsGenericType
                && type.Name == root.Name + "Asserts"
                && type.Namespace == root.Namespace
            )
            .ToArray();
}
