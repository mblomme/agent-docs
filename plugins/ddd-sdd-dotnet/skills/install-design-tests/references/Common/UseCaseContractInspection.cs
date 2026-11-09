using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Company.BoundedContext.DesignTests.Common;

internal static class UseCaseContractInspection
{
    private const BindingFlags Members =
        BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Instance
        | BindingFlags.Static
        | BindingFlags.DeclaredOnly;

    public static IEnumerable<string> Contracts(
        IEnumerable<Type> types,
        string drivingNamespace,
        Func<Type, IEnumerable<string>> inspect,
        int? arity = null,
        string interfaceName = "IUseCase"
    )
    {
        var all = types.ToArray();
        foreach (
            var selectedArity in arity.HasValue
                ? new[] { arity.Value }
                : new[] { 1, 2 }
        )
        {
            var name = $"{drivingNamespace}.{interfaceName}`{selectedArity}";
            var candidates = all.Where(t => t.FullName == name).ToArray();
            if (candidates.Length != 1)
            {
                yield return $"{name}: expected exactly one use-case port; found {candidates.Length}. Declare the public generic interface in Application.Contracts.Driving.";
                continue;
            }
            foreach (var violation in inspect(candidates[0]))
                yield return violation;
        }
    }

    public static IEnumerable<string> Shape(Type type)
    {
        if (!ValidShape(type))
            yield return $"{type}: expected a public generic interface IUseCase<TInput>, IUseCase<TInput, TOutput>, or IQueryUseCase<TInput, TOutput>. Declare one or two type parameters in input-then-output order.";
    }

    public static IEnumerable<string> Execute(Type type)
    {
        if (!ValidShape(type))
        {
            foreach (var violation in Shape(type))
                yield return violation;
            yield break;
        }
        if (!InputMethods(type).Any())
            yield return $"{type}: add public instance Execute(TInput input). A non-generic overload must accept the first interface parameter by value; additional parameters are allowed.";
    }

    public static IEnumerable<string> AsyncExecute(Type type)
    {
        if (!ValidShape(type))
        {
            foreach (var violation in Shape(type))
                yield return violation;
            yield break;
        }
        var args = type.GetGenericArguments();
        var result =
            args.Length == 1
                ? typeof(Task)
                : typeof(Task<>).MakeGenericType(args[1]);
        var resultName = args.Length == 1 ? "Task" : "Task<TOutput>";
        if (!InputMethods(type).Any(m => m.ReturnType == result))
            yield return $"{type}: add {resultName} Execute(TInput input). Use the exact Task return type; synchronous returns and ValueTask do not satisfy the asynchronous contract.";
        foreach (
            var method in Methods(type).Where(m => m.Name == "ExecuteAsync")
        )
            yield return $"{type}: rename {method.DeclaringType?.Name}.ExecuteAsync to Execute; the Async suffix is forbidden on the use-case execution method.";
    }

    private static bool ValidShape(Type type) =>
        type.IsInterface
        && type.IsVisible
        && type.IsGenericTypeDefinition
        && type.GetGenericArguments().Length is 1 or 2;

    private static IEnumerable<MethodInfo> Methods(Type type) =>
        type.GetInterfaces()
            .Append(type)
            .SelectMany(t => t.GetMethods(Members));

    private static IEnumerable<MethodInfo> InputMethods(Type type) =>
        Methods(type)
            .Where(m =>
                m.Name == "Execute"
                && m.IsPublic
                && !m.IsStatic
                && !m.IsGenericMethod
                && m.GetParameters()
                    .Any(p => p.ParameterType == type.GetGenericArguments()[0])
            );

    public static IEnumerable<string> DrivingFolder(
        string outputDirectory,
        string testAssemblyName,
        string applicationAssemblyName,
        int? arity = null,
        string interfaceName = "IUseCase"
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

        var directory = Path.GetDirectoryName(project)!;
        var sources = new List<(string Path, string Text)>();
        string? error = null;
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
                        .Any(p => p is "bin" or "obj" or ".git")
                )
                    continue;
                sources.Add((relative, File.ReadAllText(path)));
            }
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException)
        {
            error =
                $"Cannot read Application source files ({exception.GetType().Name}). Run from a readable source checkout to verify Contracts/Driving placement.";
        }
        if (error is not null)
        {
            yield return error;
            yield break;
        }
        foreach (
            var violation in DrivingDeclarations(
                sources,
                applicationAssemblyName + ".Contracts.Driving",
                arity,
                interfaceName
            )
        )
            yield return violation;
    }

    public static IEnumerable<string> DrivingDeclarations(
        IEnumerable<(string Path, string Text)> sources,
        string drivingNamespace,
        int? arity = null,
        string interfaceName = "IUseCase"
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
                        d.Identifier.ValueText == interfaceName
                        && !d.Ancestors().OfType<TypeDeclarationSyntax>().Any()
                    )
                    .Select(d =>
                        (
                            source.Path,
                            Arity: d.TypeParameterList?.Parameters.Count ?? 0,
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
            .ToArray();
        foreach (
            var selectedArity in arity.HasValue
                ? new[] { arity.Value }
                : new[] { 1, 2 }
        )
        {
            var matches = declarations
                .Where(d =>
                    d.Arity == selectedArity && d.Namespace == drivingNamespace
                )
                .ToArray();
            if (matches.Length == 0)
                yield return $"{drivingNamespace}.{interfaceName}`{selectedArity}: missing source declaration. Declare the interface directly in Contracts/Driving with the matching namespace; filenames alone do not establish placement.";
            foreach (var match in matches)
                if (
                    Path.GetDirectoryName(
                        match
                            .Path.Replace('\\', Path.DirectorySeparatorChar)
                            .Replace('/', Path.DirectorySeparatorChar)
                    ) != Path.Combine("Contracts", "Driving")
                )
                    yield return $"{match.Path}: move the {interfaceName}`{selectedArity} declaration directly to Contracts/Driving; namespace agreement alone does not establish physical placement.";
        }
    }
}
