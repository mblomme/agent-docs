using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Company.BoundedContext.DesignTests.Common;

internal static class ApplicationSourceCompilation
{
    public static IReadOnlyList<T> Checkout<T>(
        string assemblyName,
        Func<
            CSharpCompilation,
            string,
            string,
            string,
            IReadOnlyList<T>
        > inspect,
        Func<string, T> analysis
    )
    {
        string? project = null;
        var errors = DomainProjectInspection
            .Project(
                AppContext.BaseDirectory,
                typeof(ApplicationSourceCompilation).Assembly.GetName().Name!,
                assemblyName,
                path => project = path,
                "Application"
            )
            .Select(analysis)
            .ToArray();
        if (project is null || errors.Length != 0)
            return errors;
        try
        {
            return inspect(
                Load(project, assemblyName),
                assemblyName,
                Path.GetDirectoryName(project)!,
                "DddToolkit.Application"
            );
        }
        catch (Exception error)
            when (error is IOException or UnauthorizedAccessException)
        {
            return
            [
                analysis(
                    $"Cannot read Application sources ({error.GetType().Name}). Run from a readable source checkout."
                ),
            ];
        }
    }

    public static CSharpCompilation Load(string project, string assemblyName)
    {
        var directory = Path.GetDirectoryName(project)!;
        var trees = Directory
            .EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
                !Path.GetRelativePath(directory, path)
                    .Split(Path.DirectorySeparatorChar)
                    .Any(part => part is "bin" or "obj")
            )
            .Select(path =>
                CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path)
            );
        // Mirror the SDK's implicit usings without including generated assembly attributes.
        if (
            System
                .Xml.Linq.XDocument.Load(project)
                .Descendants()
                .Any(element =>
                    element.Name.LocalName == "ImplicitUsings"
                    && element.Value == "enable"
                )
        )
            trees = trees.Append(
                CSharpSyntaxTree.ParseText(
                    """
                    global using System;
                    global using System.Collections.Generic;
                    global using System.IO;
                    global using System.Linq;
                    global using System.Net.Http;
                    global using System.Threading;
                    global using System.Threading.Tasks;
                    """,
                    path: "ImplicitUsings.g.cs"
                )
            );
        var references = (
            (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!
        )
            .Split(Path.PathSeparator)
            .Concat(
                AssemblyDiscovery
                    .ProductionAssemblies(
                        typeof(ApplicationSourceCompilation).Assembly
                    )
                    .Select(a => a.Location)
            )
            .Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        return CSharpCompilation.Create(
            assemblyName,
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }
}
