using System.Xml.Linq;

namespace Company.BoundedContext.DesignTests.Common;

internal static class DomainProjectInspection
{
    public static IEnumerable<string> ToolkitSources(
        string outputDirectory,
        string testAssemblyName,
        string domainAssemblyName,
        string? requiredFile = null
    )
    {
        string? project = null;
        foreach (
            var violation in Project(
                outputDirectory,
                testAssemblyName,
                domainAssemblyName,
                path => project = path
            )
        )
            yield return violation;
        if (project is null)
            yield break;
        if (
            Directory.Exists(
                Path.Combine(Path.GetDirectoryName(project)!, "Shared")
            )
        )
            yield return "Domain/Shared is forbidden. Move reusable domain abstractions to lib/DddToolkit.Domain and remove the Shared folder.";
        foreach (
            var violation in ToolkitProjectInspection.Inspect(
                project,
                "Domain",
                requiredFile
            )
        )
            yield return violation;
    }

    public static IEnumerable<string> Project(
        string outputDirectory,
        string testAssemblyName,
        string domainAssemblyName,
        Action<string> found,
        string layer = "Domain"
    )
    {
        var directory = new DirectoryInfo(outputDirectory);
        string? testProject = null;
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                testAssemblyName + ".csproj"
            );
            if (File.Exists(candidate))
            {
                testProject = candidate;
                break;
            }
            directory = directory.Parent;
        }
        if (testProject is null)
        {
            yield return $"{testAssemblyName}.csproj: cannot locate the test project above its output directory. Run the tests from a source checkout with the project available.";
            yield break;
        }

        string[] projects;
        string? readError = null;
        try
        {
            projects = ToolkitProjectInspection
                .ProjectGraph(testProject)
                .Skip(1)
                .Where(path => ProjectAssemblyName(path) == domainAssemblyName)
                .Distinct()
                .ToArray();
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or System.Xml.XmlException
                        or ArgumentException
            )
        {
            readError =
                $"{testAssemblyName}.csproj: cannot read relative project references ({exception.GetType().Name}). Repair the project XML and reference paths.";
            projects = [];
        }
        if (readError is not null)
        {
            yield return readError;
            yield break;
        }
        if (projects.Length != 1)
        {
            yield return $"{testAssemblyName}.csproj: expected exactly one existing project reference for {layer} assembly {domainAssemblyName}; found {projects.Length}. Add or repair the relative {layer} ProjectReference.";
            yield break;
        }
        found(projects[0]);
    }

    public static IEnumerable<string> AggregateAssertions(
        string outputDirectory,
        string testAssemblyName,
        string domainAssemblyName,
        IEnumerable<Type> roots
    )
    {
        string? project = null;
        foreach (
            var violation in Project(
                outputDirectory,
                testAssemblyName,
                domainAssemblyName,
                path => project = path
            )
        )
            yield return violation;
        if (project is null)
            yield break;
        foreach (var root in roots)
        {
            var prefix = domainAssemblyName + ".";
            if (
                root.Namespace is null
                || !root.Namespace.StartsWith(prefix, StringComparison.Ordinal)
            )
            {
                yield return $"{root.Name}: cannot map its namespace to an aggregate folder beneath {domainAssemblyName}. Use the conventional Domain namespace and folder layout.";
                continue;
            }
            var relative = root.Namespace[prefix.Length..]
                .Replace('.', Path.DirectorySeparatorChar);
            foreach (var name in new[] { root.Name, root.Name + "Asserts" })
                if (
                    !File.Exists(
                        Path.Combine(
                            Path.GetDirectoryName(project)!,
                            relative,
                            name + ".cs"
                        )
                    )
                )
                    yield return $"{root.Name}: missing {relative}/{name}.cs. Place {root.Name}.cs and {root.Name}Asserts.cs together in the aggregate's namespace-relative folder; namespace agreement alone does not establish physical placement.";
        }
    }

    internal static string ProjectAssemblyName(string path) =>
        XDocument
            .Load(path)
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "AssemblyName")
            ?.Value.Trim()
            is { Length: > 0 } name
            ? name
            : Path.GetFileNameWithoutExtension(path);
}
