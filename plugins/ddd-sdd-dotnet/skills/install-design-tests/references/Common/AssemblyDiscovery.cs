using System.Reflection;

namespace Company.BoundedContext.DesignTests.Common;

internal static class AssemblyDiscovery
{
    public static Assembly[] ProductionAssemblies(Assembly testAssembly) =>
        testAssembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => a.Key == "DddProductionAssembly")
            .Select(a => Load(a.Value!))
            .SelectMany(WithToolkitDependencies)
            .Distinct()
            .ToArray();

    private static IEnumerable<Assembly> WithToolkitDependencies(
        Assembly assembly
    )
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<Assembly>();
        pending.Enqueue(assembly);
        while (pending.TryDequeue(out var current))
        {
            if (!visited.Add(current.FullName!))
                continue;
            yield return current;
            foreach (
                var reference in current
                    .GetReferencedAssemblies()
                    .Where(r =>
                        r.Name
                            is "DddToolkit.Domain"
                                or "DddToolkit.Application"
                    )
            )
                pending.Enqueue(Load(reference.FullName));
        }
    }

    internal static Assembly Load(string name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch (Exception exception)
            when (exception
                    is FileNotFoundException
                        or FileLoadException
                        or BadImageFormatException
            )
        {
            DiagnosticAssertions.AssertNoViolations(
                "Production assembly discovery",
                [
                    $"Cannot load {new AssemblyName(name).Name} ({exception.GetType().Name}).",
                ],
                "Repair the project references and rebuild the solution so production and toolkit assemblies are available in the test output."
            );
            throw;
        }
    }

    public static Assembly ToolkitDomainAssembly(
        IEnumerable<Assembly> assemblies
    ) => ToolkitAssembly(assemblies, "Domain");

    public static Assembly ToolkitAssembly(
        IEnumerable<Assembly> assemblies,
        string layer
    ) =>
        Required(
            assemblies,
            a => a.GetName().Name == "DddToolkit." + layer,
            "DddToolkit." + layer,
            "Create lib/DddToolkit."
                + layer
                + "/DddToolkit."
                + layer
                + ".csproj and reference it from the product "
                + layer
                + " project. Rebuild the solution."
        );

    public static Assembly ApplicationAssembly(
        IEnumerable<Assembly> assemblies
    ) =>
        Required(
            assemblies,
            a =>
                a.GetName().Name is { } name
                && name.EndsWith(".Application", StringComparison.Ordinal)
                && name != "DddToolkit.Application",
            "product .Application",
            "Reference exactly one product Application project; DddToolkit.Application is discovered separately."
        );

    private static Assembly Required(
        IEnumerable<Assembly> assemblies,
        Func<Assembly, bool> predicate,
        string name,
        string action
    )
    {
        var candidates = assemblies.Distinct().Where(predicate).ToArray();
        DiagnosticAssertions.AssertNoViolations(
            "Assembly discovery",
            candidates.Length == 1
                ? []
                :
                [
                    $"Expected exactly one {name} assembly; found {candidates.Length}.",
                ],
            action
        );
        return candidates[0];
    }

    public static Assembly DomainAssembly(IEnumerable<Assembly> assemblies)
    {
        var candidates = assemblies
            .Where(a =>
                a.GetName().Name?.Split('.').Last() == "Domain"
                && a.GetName().Name != "DddToolkit.Domain"
            )
            .ToArray();
        DiagnosticAssertions.AssertNoViolations(
            "Domain assembly discovery",
            candidates.Length == 1
                ? []
                :
                [
                    $"Expected exactly one production assembly named with the .Domain suffix; found {candidates.Length}.",
                ],
            "Reference the production projects in the test project and ensure exactly one assembly has the .Domain suffix. Empty assemblies are supported."
        );
        return candidates[0];
    }
}
