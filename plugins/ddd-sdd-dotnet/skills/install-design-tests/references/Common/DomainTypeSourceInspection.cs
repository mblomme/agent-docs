using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Company.BoundedContext.DesignTests.Common;

internal static class DomainTypeSourceInspection
{
    public static IEnumerable<string> OnePerFile(
        string outputDirectory,
        string testAssemblyName,
        string domainAssemblyName,
        IEnumerable<Type> concreteTypes,
        string kind
    )
    {
        var types = concreteTypes.ToArray();
        if (types.Length == 0)
            return [];

        return InspectSources(
            outputDirectory,
            testAssemblyName,
            domainAssemblyName,
            sources =>
                OnePerFile(types.Select(type => type.FullName!), sources, kind),
            kind
        );
    }

    internal static IEnumerable<string> InspectSources(
        string outputDirectory,
        string testAssemblyName,
        string domainAssemblyName,
        Func<
            IEnumerable<(string Path, string Text)>,
            IEnumerable<string>
        > inspect,
        string kind
    )
    {
        string? project = null;
        foreach (
            var violation in DomainProjectInspection.Project(
                outputDirectory,
                testAssemblyName,
                domainAssemblyName,
                path => project = path
            )
        )
            yield return violation;
        if (project is null)
            yield break;
        var directory = Path.GetDirectoryName(project)!;
        var sources = new List<(string Path, string Text)>();
        string? failure = null;
        try
        {
            foreach (
                var path in Directory.EnumerateFiles(
                    directory,
                    "*.cs",
                    SearchOption.AllDirectories
                )
            )
            {
                var relative = Path.GetRelativePath(directory, path);
                if (
                    relative
                        .Split(Path.DirectorySeparatorChar)
                        .Any(part => part is "bin" or "obj" or ".git")
                )
                    continue;
                sources.Add((relative, File.ReadAllText(path)));
            }
        }
        catch (Exception error)
            when (error is IOException or UnauthorizedAccessException)
        {
            failure =
                $"Cannot read Domain source files ({error.GetType().Name}). Run from a readable source checkout to verify {kind} file placement.";
        }
        if (failure is not null)
        {
            yield return failure;
            yield break;
        }
        foreach (var violation in inspect(sources))
            yield return violation;
    }

    public static IEnumerable<string> OnePerFile(
        IEnumerable<string> concreteNames,
        IEnumerable<(string Path, string Text)> sources,
        string kind
    )
    {
        var expected = concreteNames.ToHashSet(StringComparer.Ordinal);
        if (expected.Count == 0)
            yield break;
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            var declarations = Declarations(source.Text)
                .Where(expected.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            found.UnionWith(declarations);
            if (declarations.Length > 1)
                yield return $"{source.Path}: contains multiple concrete {kind} types: {string.Join(", ", declarations)}. Move each concrete {kind} to a separate source file; do not bundle them.";
        }
        foreach (var missing in expected.Except(found, StringComparer.Ordinal))
            yield return $"{missing}: cannot locate its source declaration. Include its readable .cs source in the Domain checkout so the one-concrete-{kind}-per-file rule can be verified.";
    }

    public static IEnumerable<string> IdentityInSameFile(
        string outputDirectory,
        string testAssemblyName,
        string domainAssemblyName,
        IEnumerable<Type> owners,
        Type entityBase
    )
    {
        var concreteOwners = owners.ToArray();
        if (concreteOwners.Length == 0)
            return [];

        return InspectSources(
            outputDirectory,
            testAssemblyName,
            domainAssemblyName,
            sources =>
                IdentityInSameFile(
                    concreteOwners.Select(owner =>
                    {
                        var current = owner;
                        while (
                            !(
                                current.IsGenericType
                                && current.GetGenericTypeDefinition()
                                    == entityBase
                            )
                        )
                            current = current.BaseType!;
                        return (
                            owner.FullName!,
                            current.GetGenericArguments()[0].FullName!
                        );
                    }),
                    sources
                ),
            "entity identity"
        );
    }

    public static IEnumerable<string> IdentityInSameFile(
        IEnumerable<(string Owner, string Id)> identities,
        IEnumerable<(string Path, string Text)> sources
    )
    {
        var pairs = identities.ToArray();
        if (pairs.Length == 0)
            yield break;

        var declarations = sources
            .Select(source =>
                (
                    source.Path,
                    Names: Declarations(source.Text)
                        .ToHashSet(StringComparer.Ordinal)
                )
            )
            .ToArray();
        foreach (var (owner, id) in pairs)
        {
            var ownerFiles = declarations
                .Where(source => source.Names.Contains(owner))
                .ToArray();
            if (ownerFiles.Length == 0)
                yield return $"{owner}: cannot locate its source declaration to verify identity file placement.";
            else if (!ownerFiles.Any(source => source.Names.Contains(id)))
                yield return $"{owner}: declare identity type {id} in the same source file as its owner ({string.Join(", ", ownerFiles.Select(source => source.Path))}).";
        }
    }

    public static IEnumerable<string> RepositoriesBesideAggregates(
        IEnumerable<(string Repository, string Aggregate)> repositories,
        IEnumerable<(string Path, string Text)> sources
    )
    {
        var declarations = sources
            .SelectMany(source =>
                Declarations(source.Text)
                    .Select(name => (Name: name, source.Path))
            )
            .ToArray();
        foreach (var (repository, aggregate) in repositories)
        {
            var repositoryFiles = declarations
                .Where(d => d.Name == repository)
                .Select(d => d.Path)
                .Distinct()
                .ToArray();
            var aggregateFiles = declarations
                .Where(d => d.Name == aggregate)
                .Select(d => d.Path)
                .Distinct()
                .ToArray();
            if (repositoryFiles.Length == 0 || aggregateFiles.Length == 0)
            {
                yield return $"{repository}: cannot locate repository or aggregate {aggregate} source declarations to verify placement.";
                continue;
            }
            foreach (var file in repositoryFiles)
                if (
                    aggregateFiles.Contains(file)
                    || !aggregateFiles.Any(owner =>
                        Path.GetDirectoryName(owner)
                        == Path.GetDirectoryName(file)
                    )
                )
                    yield return $"{repository} ({file}): place the interface in a separate source file in the same folder as {aggregate} ({string.Join(", ", aggregateFiles)}).";
            foreach (var file in repositoryFiles)
                if (
                    declarations.Any(d =>
                        d.Path == file && d.Name != repository
                    )
                )
                    yield return $"{repository} ({file}): declare the repository interface in its own source file.";
        }
    }

    public static IEnumerable<string> ConcreteValueObjectsOutsideShared(
        IEnumerable<string> concreteNames,
        IEnumerable<(string Path, string Text)> sources
    )
    {
        ArgumentNullException.ThrowIfNull(concreteNames);
        ArgumentNullException.ThrowIfNull(sources);
        var expected = concreteNames.ToHashSet(StringComparer.Ordinal);
        var found = new HashSet<string>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            var names = Declarations(source.Text)
                .Where(expected.Contains)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            found.UnionWith(names);
            var folder = source.Path.Replace('\\', '/').Split('/')[0];
            if (
                !string.Equals(
                    folder,
                    "Shared",
                    StringComparison.OrdinalIgnoreCase
                )
            )
                continue;
            foreach (var name in names)
                yield return $"{name} ({source.Path}): concrete value objects must not reside in Domain/Shared. Carefully read the domain-layer specification again for correct folder locations";
        }
        foreach (var name in expected.Except(found, StringComparer.Ordinal))
            yield return $"{name}: cannot locate its source declaration to verify folder placement. Carefully read the domain-layer specification again for correct folder locations";
    }

    public static IEnumerable<string> AggregateValueObjects(
        IEnumerable<Type> roots,
        IEnumerable<Type> valueObjects,
        IEnumerable<(string Path, string Text)> sources,
        string domainAssemblyName
    ) =>
        AggregateValueObjects(
            roots.Select(root => (root.Name, root.Namespace)),
            valueObjects.Select(type => type.FullName!),
            sources,
            domainAssemblyName
        );

    internal static IEnumerable<string> AggregateValueObjects(
        IEnumerable<(string Name, string? Namespace)> roots,
        IEnumerable<string> valueObjects,
        IEnumerable<(string Path, string Text)> sources,
        string domainAssemblyName
    )
    {
        var concreteNames = valueObjects.ToHashSet(StringComparer.Ordinal);
        var files = sources.ToArray();
        foreach (var root in roots)
        {
            var prefix = domainAssemblyName + ".";
            if (
                root.Namespace is null
                || !root.Namespace.StartsWith(prefix, StringComparison.Ordinal)
            )
            {
                yield return $"{root.Name}: cannot map its namespace to an aggregate folder.";
                continue;
            }

            var folder =
                root.Namespace[prefix.Length..].Replace('.', '/')
                + "/ValueObjects";
            var count = files
                .Where(file =>
                    string.Equals(
                        Path.GetDirectoryName(file.Path.Replace('\\', '/'))
                            ?.Replace('\\', '/'),
                        folder,
                        StringComparison.Ordinal
                    )
                )
                .SelectMany(file => Declarations(file.Text))
                .Where(concreteNames.Contains)
                .Distinct(StringComparer.Ordinal)
                .Count();
            if (count < 4 || count > 100)
                yield return $"{root.Name}: {folder}/ contains {count} concrete value objects; Maximize the use of value objects: every public property of an aggregate or entity is a value object candidate.";
        }
    }

    private static IEnumerable<string> Declarations(string source) =>
        CSharpSyntaxTree
            .ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Select(MetadataName);

    private static string MetadataName(TypeDeclarationSyntax declaration)
    {
        var namespaces = declaration
            .Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Reverse()
            .Select(node =>
                node.Name.ToString()
                    .Replace("global::", "", StringComparison.Ordinal)
            );
        var types = declaration
            .Ancestors()
            .OfType<TypeDeclarationSyntax>()
            .Reverse()
            .Append(declaration)
            .Select(node =>
                node.Identifier.ValueText
                + (
                    node.TypeParameterList is { } parameters
                        ? "`"
                            + parameters.Parameters.Count.ToString(
                                System
                                    .Globalization
                                    .CultureInfo
                                    .InvariantCulture
                            )
                        : ""
                )
            );
        var prefix = string.Join(".", namespaces);
        return (prefix.Length == 0 ? "" : prefix + ".")
            + string.Join("+", types);
    }
}
