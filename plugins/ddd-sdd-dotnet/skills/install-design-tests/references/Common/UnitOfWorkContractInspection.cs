using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Company.BoundedContext.DesignTests.Common;

internal static class UnitOfWorkContractInspection
{
    public static IEnumerable<string> Contracts(
        IEnumerable<Type> types,
        string drivenNamespace,
        Func<Type, IEnumerable<string>> inspect
    )
    {
        var name = drivenNamespace + ".IUnitOfWork";
        var candidates = types.Where(t => t.FullName == name).ToArray();
        if (candidates.Length != 1)
        {
            yield return $"{name}: expected exactly one unit-of-work port; found {candidates.Length}. Declare public interface IUnitOfWork in Application.Contracts.Driven.";
            yield break;
        }
        foreach (var violation in inspect(candidates[0]))
            yield return violation;
    }

    public static IEnumerable<string> Shape(Type type)
    {
        if (
            !type.IsInterface
            || !type.IsPublic
            || type.IsGenericType
            || type.IsNested
        )
            yield return $"{type}: expected a public nongeneric top-level interface IUnitOfWork in Application.Contracts.Driven.";
    }

    public static IEnumerable<string> Do(Type type) =>
        Operation(
            type,
            "Do",
            "Task Do()",
            m =>
                !m.IsGenericMethod
                && m.GetParameters().Length == 0
                && m.ReturnType == typeof(Task)
        );

    public static IEnumerable<string> Save(
        Type type,
        Type aggregateContract,
        Type repositoryContract
    ) =>
        Operation(
            type,
            "Save",
            "Task Save<TRepository>(IAggregateRoot aggregate) where TRepository : IRepository",
            m =>
                RepositoryParameter(m, repositoryContract) is not null
                && m.ReturnType == typeof(Task)
                && m.GetParameters() is [var input]
                && input.ParameterType == aggregateContract
        );

    public static IEnumerable<string> Repo(
        Type type,
        Type repositoryContract
    ) =>
        Operation(
            type,
            "Repo",
            "TRepository Repo<TRepository>() where TRepository : IRepository",
            m =>
                RepositoryParameter(m, repositoryContract) is { } repository
                && m.GetParameters().Length == 0
                && m.ReturnType == repository
        );

    private static Type? RepositoryParameter(
        MethodInfo method,
        Type repositoryContract
    )
    {
        if (
            !method.IsGenericMethodDefinition
            || method.GetGenericArguments() is not [var repository]
        )
            return null;
        return
            repository.GenericParameterAttributes
                == GenericParameterAttributes.None
            && repository.GetGenericParameterConstraints() is [var constraint]
            && constraint == repositoryContract
            ? repository
            : null;
    }

    private static IEnumerable<string> Operation(
        Type type,
        string name,
        string signature,
        Func<MethodInfo, bool> valid
    )
    {
        var shape = Shape(type).ToArray();
        if (shape.Length != 0)
        {
            foreach (var violation in shape)
                yield return violation;
            yield break;
        }
        var methods = type.GetInterfaces()
            .Append(type)
            .SelectMany(t =>
                t.GetMethods(
                    BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.Instance
                        | BindingFlags.Static
                        | BindingFlags.DeclaredOnly
                )
            );
        if (
            !methods.Any(m =>
                m.Name == name && m.IsPublic && !m.IsStatic && valid(m)
            )
        )
            yield return $"{type}: add public instance {signature}. Use the exact return type, parameter count, generic arity, and repository constraint.";
    }

    public static IEnumerable<string> DrivenFolder(
        string outputDirectory,
        string testAssemblyName,
        string applicationAssemblyName
    )
    {
        string? project = null;
        foreach (
            var violation in DomainProjectInspection.Project(
                outputDirectory,
                testAssemblyName,
                applicationAssemblyName,
                path => project = path,
                "Application"
            )
        )
            yield return violation;
        if (project is null)
            yield break;
        var sources = new List<(string Path, string Text)>();
        string? error = null;
        try
        {
            var directory = Path.GetDirectoryName(project)!;
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
                    !relative
                        .Split(Path.DirectorySeparatorChar)
                        .Any(p => p is "bin" or "obj" or ".git")
                )
                    sources.Add((relative, File.ReadAllText(path)));
            }
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException)
        {
            error =
                $"Cannot read Application source files ({exception.GetType().Name}). Run from a readable source checkout to verify Contracts/Driven placement.";
        }
        if (error is not null)
        {
            yield return error;
            yield break;
        }
        foreach (
            var violation in DrivenDeclarations(
                sources,
                applicationAssemblyName + ".Contracts.Driven"
            )
        )
            yield return violation;
    }

    public static IEnumerable<string> DrivenDeclarations(
        IEnumerable<(string Path, string Text)> sources,
        string drivenNamespace
    )
    {
        var declarations = sources
            .SelectMany(source =>
                CSharpSyntaxTree
                    .ParseText(source.Text)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .Where(d =>
                        d.Identifier.ValueText == "IUnitOfWork"
                        && d.TypeParameterList is null
                        && !d.Ancestors().OfType<TypeDeclarationSyntax>().Any()
                    )
                    .Select(d =>
                        (
                            source.Path,
                            Namespace: string.Join(
                                ".",
                                d.Ancestors()
                                    .OfType<BaseNamespaceDeclarationSyntax>()
                                    .Reverse()
                                    .Select(n =>
                                        n.Name.ToString()
                                            .Replace(
                                                "global::",
                                                "",
                                                StringComparison.Ordinal
                                            )
                                    )
                            )
                        )
                    )
            )
            .Where(d => d.Namespace == drivenNamespace)
            .ToArray();
        if (declarations.Length == 0)
            yield return $"{drivenNamespace}.IUnitOfWork: missing source declaration. Declare the interface directly in Contracts/Driven with the matching namespace; filenames alone do not establish placement.";
        foreach (var declaration in declarations)
            if (
                Path.GetDirectoryName(
                    declaration
                        .Path.Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar)
                ) != Path.Combine("Contracts", "Driven")
            )
                yield return $"{declaration.Path}: move the IUnitOfWork declaration directly to Contracts/Driven; namespace agreement alone does not establish physical placement.";
    }
}
