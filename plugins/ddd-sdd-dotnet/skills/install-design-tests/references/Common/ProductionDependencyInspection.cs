using Mono.Cecil;

namespace Company.BoundedContext.DesignTests.Common;

internal static class ProductionDependencyInspection
{
    public static string[] References(
        Stream assembly,
        IEnumerable<string> productionNames
    )
    {
        var names = productionNames.ToHashSet(StringComparer.Ordinal);
        using var module = ModuleDefinition.ReadModule(assembly);
        return module
            .GetTypeReferences()
            .Where(t =>
                t.Scope is AssemblyNameReference scope
                && names.Contains(scope.Name)
            )
            .Select(t =>
                $"{t.FullName}: direct production type dependency in {t.Scope.Name}. Discover and inspect the type at runtime instead."
            )
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
