using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Company.BoundedContext.DesignTests.Common;

internal static class ToolkitProjectInspection
{
    internal static readonly string[] DomainContracts =
    [
        "Entity`1",
        "AggregateRoot`1",
        "IAggregateRoot",
        "IDomainEvent",
        "BaseDomainEvent",
        "IEntityId",
        "IEntityId`1",
        "EntityId",
        "ValueObject",
        "IRepository",
        "IRepository`2",
        "DomainAsserts",
        "DomainException",
    ];

    internal static readonly string[] ApplicationContracts =
    [
        "IUseCase`1",
        "IUseCase`2",
        "IQueryUseCase`2",
        "IPolicy`1",
        "IQuery`1",
        "IQuery`2",
        "IUnitOfWork",
    ];

    public static IEnumerable<string> ProjectGraph(string project)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<string>();
        pending.Enqueue(Path.GetFullPath(project));
        while (pending.TryDequeue(out var current))
        {
            if (!visited.Add(current) || !File.Exists(current))
                continue;
            yield return current;
            foreach (var reference in References(current))
                pending.Enqueue(reference);
        }
    }

    internal static IEnumerable<string> References(string project) =>
        XDocument
            .Load(project)
            .Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => (string?)e.Attribute("Include"))
            .OfType<string>()
            .Select(path =>
                Path.GetFullPath(
                    Path.Combine(
                        Path.GetDirectoryName(project)!,
                        path.Replace('\\', Path.DirectorySeparatorChar)
                            .Replace('/', Path.DirectorySeparatorChar)
                    )
                )
            );

    public static IEnumerable<string> Inspect(
        string productProject,
        string layer,
        string? requiredFile = null
    )
    {
        var findings = new List<string>();
        try
        {
            InspectCore();
        }
        catch (Exception exception)
            when (exception
                    is IOException
                        or UnauthorizedAccessException
                        or System.Xml.XmlException
                        or ArgumentException
            )
        {
            findings.Add(
                $"DddToolkit.{layer}: cannot inspect project references or source ({exception.GetType().Name}). Repair the project XML and reference paths in the source checkout."
            );
        }
        return findings;

        void InspectCore()
        {
            // Product projects reside in src/<project>; lib is its repository-root sibling.
            var root = Directory
                .GetParent(Path.GetDirectoryName(productProject)!)!
                .Parent!.FullName;
            var name = "DddToolkit." + layer;
            var expected = Path.Combine(root, "lib", name, name + ".csproj");
            var projects = ProjectGraph(productProject).ToArray();
            if (!File.Exists(expected))
            {
                findings.Add(
                    $"Missing lib/{name}/{name}.csproj. Create the {name} class library and reference it from the product {layer} project."
                );
                return;
            }
            if (!projects.Contains(expected, StringComparer.Ordinal))
                findings.Add(
                    $"{name}: missing project reference. Reference lib/{name}/{name}.csproj from the product {layer} project."
                );
            var candidates = projects
                .Where(path =>
                    DomainProjectInspection.ProjectAssemblyName(path) == name
                )
                .ToArray();
            if (
                candidates.Length != 1
                || candidates[0] != expected
                || DomainProjectInspection.ProjectAssemblyName(expected) != name
            )
                findings.Add(
                    $"{name}: expected exactly one referenced assembly at lib/{name}/{name}.csproj with AssemblyName {name}."
                );
            foreach (var dependency in ProjectGraph(expected).Skip(1))
                if (
                    !(
                        layer == "Application"
                        && dependency
                            == Path.Combine(
                                root,
                                "lib",
                                "DddToolkit.Domain",
                                "DddToolkit.Domain.csproj"
                            )
                    )
                )
                    findings.Add(
                        $"{name}: forbidden project dependency {Path.GetFileName(dependency)}. Toolkit libraries must be project agnostic; only DddToolkit.Application may reference DddToolkit.Domain."
                    );
            foreach (
                var project in ProjectGraph(expected)
                    .Append(productProject)
                    .Distinct()
            )
            foreach (
                var reference in References(project)
                    .Where(path => !File.Exists(path))
            )
                findings.Add(
                    $"{Path.GetFileName(project)}: broken ProjectReference to {Path.GetFileName(reference)}. Repair the relative reference path."
                );
            var directory = Path.GetDirectoryName(expected)!;
            var declarations = Sources(directory)
                .SelectMany(path =>
                    CSharpSyntaxTree
                        .ParseText(File.ReadAllText(path))
                        .GetRoot()
                        .DescendantNodes()
                        .OfType<BaseTypeDeclarationSyntax>()
                        .Select(node =>
                            (Path: path, Node: node, Name: MetadataName(node))
                        )
                )
                .ToArray();
            var contracts =
                layer == "Domain" ? DomainContracts : ApplicationContracts;
            foreach (
                var contract in contracts.Where(c =>
                    requiredFile is null || FileFor(c) == requiredFile
                )
            )
            {
                var matches = declarations
                    .Where(d => d.Name == contract)
                    .ToArray();
                var folder =
                    layer == "Domain"
                        ? ""
                        : Path.Combine(
                            "Contracts",
                            contract.StartsWith(
                                "IQuery`",
                                StringComparison.Ordinal
                            )
                            || contract == "IUnitOfWork"
                                ? "Driven"
                                : "Driving"
                        );
                var ns =
                    name
                    + (
                        folder.Length == 0
                            ? ""
                            : "."
                                + folder.Replace(
                                    Path.DirectorySeparatorChar,
                                    '.'
                                )
                    );
                if (matches.Length != 1)
                    findings.Add(
                        $"{name}: expected exactly one {contract} declaration; found {matches.Length}. Declare it in {ns}."
                    );
                foreach (var match in matches)
                {
                    var actualNamespace = string.Join(
                        ".",
                        match
                            .Node.Ancestors()
                            .OfType<BaseNamespaceDeclarationSyntax>()
                            .Reverse()
                            .Select(n => n.Name.ToString())
                    );
                    var relative = Path.GetRelativePath(directory, match.Path);
                    var validFile =
                        layer == "Application"
                            ? Path.GetDirectoryName(relative) == folder
                            : relative == FileFor(contract)
                                || relative == contract.Split('`')[0] + ".cs";
                    if (
                        !validFile
                        || actualNamespace != ns
                        || match
                            .Node.Ancestors()
                            .OfType<TypeDeclarationSyntax>()
                            .Any()
                    )
                        findings.Add(
                            $"{contract}: place the source declaration in lib/{name}/{Path.Combine(folder, FileFor(contract)).Replace('\\', '/')} with namespace {ns}."
                        );
                }
            }
        }
    }

    internal static IEnumerable<string> Sources(string directory) =>
        Directory
            .EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
                !Path.GetRelativePath(directory, path)
                    .Split(Path.DirectorySeparatorChar)
                    .Any(p => p is "bin" or "obj")
            );

    internal static string MetadataName(BaseTypeDeclarationSyntax node) =>
        node.Identifier.ValueText
        + (
            node is TypeDeclarationSyntax { TypeParameterList: { } parameters }
                ? "`" + parameters.Parameters.Count
                : ""
        );

    private static string FileFor(string contract) =>
        contract.Split('`')[0] switch
        {
            "IAggregateRoot" or "IDomainEvent" => "AggregateRoot.cs",
            "IEntityId" => "EntityId.cs",
            var name => name + ".cs",
        };
}
