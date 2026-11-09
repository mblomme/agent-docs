using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Company.BoundedContext.DesignTests.Common;

internal static class ApplicationWorkflowInspection
{
    internal sealed record Violation(string Rule, string Message);

    private sealed record Workflow(
        INamedTypeSymbol Type,
        string Kind,
        INamedTypeSymbol[] Ports
    );

    public static IReadOnlyList<Violation> Checkout(string assemblyName) =>
        ApplicationSourceCompilation.Checkout(
            assemblyName,
            Inspect,
            message => new Violation("analysis", message)
        );

    public static IReadOnlyList<Violation> Inspect(
        CSharpCompilation compilation,
        string applicationNamespace,
        string root,
        string? contractNamespace = null
    )
    {
        var violations = compilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => new Violation(
                "analysis",
                d
                    + " Repair the Application compilation before checking workflows."
            ))
            .ToList();
        if (violations.Count != 0)
            return violations;
        contractNamespace ??= applicationNamespace;
        var definitions = new Dictionary<string, INamedTypeSymbol[]>
        {
            ["command"] = Resolve("IUseCase`1", "IUseCase`2"),
            ["query"] = Resolve("IQueryUseCase`2"),
            ["policy"] = Resolve("IPolicy`1"),
        };
        var unitOfWork = compilation.GetTypeByMetadataName(
            contractNamespace + ".Contracts.Driven.IUnitOfWork"
        );
        if (
            definitions.Values.Any(ports => ports.Length == 0)
            || unitOfWork is null
        )
            return
            [
                new(
                    "analysis",
                    "Cannot resolve workflow or unit-of-work contracts. Declare and reference the driving and driven interfaces in lib/DddToolkit.Application before inspecting workflows."
                ),
            ];

        var types = compilation
            .SyntaxTrees.SelectMany(tree =>
                tree.GetRoot()
                    .DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .Select(node =>
                        compilation
                            .GetSemanticModel(tree)
                            .GetDeclaredSymbol(node)
                    )
            )
            .OfType<INamedTypeSymbol>()
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
            .Where(type =>
                type.TypeKind == TypeKind.Class
                && !type.IsAbstract
                && !type.IsStatic
            )
            .ToArray();
        var workflows = new List<Workflow>();
        foreach (var type in types)
        {
            var paths = Paths(type);
            foreach (var (kind, ports) in definitions)
            {
                var implemented = type
                    .AllInterfaces.Where(i =>
                        ports.Any(p => Same(i.OriginalDefinition, p))
                    )
                    .ToArray();
                var hasPort = type.AllInterfaces.Any(i =>
                    definitions
                        .Values.SelectMany(value => value)
                        .Any(p => Same(i.OriginalDefinition, p))
                );
                var candidate =
                    !hasPort
                    && paths.Any(path => CandidateKind(path) == kind)
                    && HasExecute(type);
                if (implemented.Length != 0 || candidate)
                    workflows.Add(new(type, kind, implemented));
            }
        }
        foreach (var workflow in workflows)
        {
            var (type, kind, ports) = workflow;
            var paths = Paths(type);
            var folder = kind switch
            {
                "query" => "Queries",
                "policy" => "Policies",
                _ => "",
            };
            var expected =
                (folder.Length == 0 ? "" : folder + "/") + type.Name + ".cs";
            if (
                paths.Length != 1
                || paths[0] != expected
                || type.ContainingType is not null
                || workflows.Any(other =>
                    !Same(other.Type, type)
                    && Paths(other.Type).Intersect(paths).Any()
                )
            )
                Report(
                    kind + "-file",
                    $"Declare one concrete {kind} in its own {expected} file directly in the required folder; do not bundle workflows or split partial declarations across files."
                );
            if (ports.Length == 0)
                Report(
                    kind + "-interface",
                    $"Implement {string.Join(" or ", definitions[kind].Select(p => p.ToDisplayString()))}, directly or through inheritance."
                );
            if (kind is "command" or "query")
                foreach (
                    var input in ports
                        .Select(p => p.TypeArguments[0])
                        .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default)
                )
                    if (
                        input.TypeKind != TypeKind.Class
                        || input.DeclaringSyntaxReferences.Length == 0
                        || input.DeclaringSyntaxReferences.Any(reference =>
                            !paths.Contains(
                                Relative(reference.SyntaxTree.FilePath)
                            )
                        )
                    )
                        Report(
                            kind + "-input",
                            $"Declare input {input.ToDisplayString()} as a class or record class in the same file as its workflow."
                        );
            if (kind != "query")
                continue;
            foreach (var output in ports.Select(p => p.TypeArguments[1]))
                QueryDataInspection.Inspect(
                    output,
                    applicationNamespace,
                    root,
                    true,
                    message => Report("query-output", message)
                );
            for (
                var current = type;
                current is not null;
                current = current.BaseType
            )
                foreach (var reference in current.DeclaringSyntaxReferences)
                {
                    var syntax = reference.GetSyntax();
                    var model = compilation.GetSemanticModel(syntax.SyntaxTree);
                    var domainReported = false;
                    var workReported = false;
                    foreach (var node in syntax.DescendantNodesAndSelf())
                    {
                        var symbol = model.GetSymbolInfo(node).Symbol;
                        var referencedType = symbol switch
                        {
                            ITypeSymbol t => t,
                            IMethodSymbol m => m.ContainingType,
                            IFieldSymbol f => f.Type,
                            IPropertySymbol p => p.Type,
                            IParameterSymbol p => p.Type,
                            ILocalSymbol l => l.Type,
                            _ => null,
                        };
                        var expressionType =
                            node is ExpressionSyntax
                                ? model.GetTypeInfo(node).Type
                                : null;
                        if (
                            !domainReported
                            && (
                                ContainsDomain(referencedType)
                                || ContainsDomain(expressionType)
                                || ContainsDomain(symbol?.ContainingType)
                                || (
                                    symbol is IMethodSymbol method
                                    && method.TypeArguments.Any(ContainsDomain)
                                )
                            )
                        )
                        {
                            Report(
                                "query-domain",
                                $"Remove the Domain dependency at {Relative(syntax.SyntaxTree.FilePath)}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}; queries must use read-only driven ports and DTOs. Inherited source members are included."
                            );
                            domainReported = true;
                        }
                        if (
                            !workReported
                            && (
                                ContainsUnitOfWork(referencedType)
                                || ContainsUnitOfWork(expressionType)
                            )
                        )
                        {
                            Report(
                                "query-unit-of-work",
                                $"Remove the IUnitOfWork dependency at {Relative(syntax.SyntaxTree.FilePath)}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}; queries must use read-only driven ports. Inherited source members are included."
                            );
                            workReported = true;
                        }
                    }
                }

            void Report(string rule, string message) =>
                violations.Add(
                    new(
                        rule,
                        $"{type.ToDisplayString()} ({string.Join(", ", paths)}): {message}"
                    )
                );
        }
        return violations.Distinct().ToArray();

        INamedTypeSymbol[] Resolve(params string[] names) =>
            names
                .Select(name =>
                    compilation.GetTypeByMetadataName(
                        contractNamespace + ".Contracts.Driving." + name
                    )
                )
                .OfType<INamedTypeSymbol>()
                .ToArray();
        string Relative(string path) =>
            Path.GetRelativePath(root, path).Replace('\\', '/');
        string[] Paths(ITypeSymbol type) =>
            type
                .DeclaringSyntaxReferences.Select(reference =>
                    Relative(reference.SyntaxTree.FilePath)
                )
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        bool ContainsDomain(ITypeSymbol? type)
        {
            var domainName =
                applicationNamespace[..^"Application".Length] + "Domain";
            return Visit(
                type,
                new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default)
            );

            bool Visit(ITypeSymbol? candidate, HashSet<ITypeSymbol> seen)
            {
                if (candidate is null || !seen.Add(candidate))
                    return false;
                var ns = candidate.ContainingNamespace?.ToDisplayString();
                return candidate.ContainingAssembly?.Name == "DddToolkit.Domain"
                    || ns == "DddToolkit.Domain"
                    || ns?.StartsWith(
                        "DddToolkit.Domain.",
                        StringComparison.Ordinal
                    ) == true
                    || candidate.ContainingAssembly?.Name == domainName
                    || ns == domainName
                    || ns?.StartsWith(
                        domainName + ".",
                        StringComparison.Ordinal
                    ) == true
                    || Visit(candidate.ContainingType, seen)
                    || (
                        candidate is IArrayTypeSymbol array
                        && Visit(array.ElementType, seen)
                    )
                    || (
                        candidate is INamedTypeSymbol named
                        && named.TypeArguments.Any(t => Visit(t, seen))
                    )
                    || (
                        candidate is ITypeParameterSymbol parameter
                        && parameter.ConstraintTypes.Any(t => Visit(t, seen))
                    );
            }
        }
        bool ContainsUnitOfWork(ITypeSymbol? type) =>
            type is not null
            && (
                Same(type, unitOfWork)
                || type.AllInterfaces.Any(i => Same(i, unitOfWork))
                || (
                    type is IArrayTypeSymbol array
                    && ContainsUnitOfWork(array.ElementType)
                )
                || (
                    type is INamedTypeSymbol named
                    && named.TypeArguments.Any(ContainsUnitOfWork)
                )
                || (
                    type is ITypeParameterSymbol parameter
                    && parameter.ConstraintTypes.Any(t =>
                        Same(t, unitOfWork)
                        || t.AllInterfaces.Any(i => Same(i, unitOfWork))
                    )
                )
            );
    }

    private static bool Same(ISymbol? left, ISymbol? right) =>
        SymbolEqualityComparer.Default.Equals(left, right);

    private static bool HasExecute(INamedTypeSymbol type)
    {
        for (
            var current = type;
            current is not null;
            current = current.BaseType
        )
            if (
                current
                    .GetMembers()
                    .OfType<IMethodSymbol>()
                    .Any(method =>
                        !method.IsStatic
                        && (
                            method.Name == "Execute"
                            || method.ExplicitInterfaceImplementations.Any(i =>
                                i.Name == "Execute"
                            )
                        )
                    )
            )
                return true;
        return false;
    }

    private static string? CandidateKind(string path)
    {
        if (!path.Contains('/', StringComparison.Ordinal))
            return "command";
        if (path.StartsWith("Queries/", StringComparison.Ordinal))
            return "query";
        if (path.StartsWith("Policies/", StringComparison.Ordinal))
            return "policy";
        return null;
    }
}
