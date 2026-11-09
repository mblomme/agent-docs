using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Company.BoundedContext.DesignTests.Common;

internal static class QueryPortInspection
{
    internal sealed record Violation(string Rule, string Message);

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
        var findings = compilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => new Violation(
                "analysis",
                d
                    + " Repair the Application compilation before checking query ports."
            ))
            .ToList();
        if (findings.Count != 0)
            return findings;
        contractNamespace ??= applicationNamespace;
        var drivenNamespace = applicationNamespace + ".Contracts.Driven";
        var definitions = Enumerable
            .Range(1, 2)
            .Select(arity =>
                compilation.GetTypeByMetadataName(
                    contractNamespace + ".Contracts.Driven.IQuery`" + arity
                )
            )
            .ToArray();
        foreach (
            var (definition, index) in definitions.Select(
                (value, index) => (value, index)
            )
        )
        {
            if (definition is null)
            {
                findings.Add(
                    new(
                        "shape",
                        $"Missing public IQuery with arity {index + 1} in {contractNamespace}.Contracts.Driven. Create and reference lib/DddToolkit.Application."
                    )
                );
                continue;
            }
            if (definition.DeclaringSyntaxReferences.Length != 0)
                Placement(definition, contractNamespace + ".Contracts.Driven");
            if (
                !PublicInterface(definition)
                || definition.Interfaces.Length != 0
                || definition.TypeParameters.Any(p =>
                    p.HasConstructorConstraint
                    || p.HasReferenceTypeConstraint
                    || p.HasValueTypeConstraint
                    || p.HasUnmanagedTypeConstraint
                    || p.HasNotNullConstraint
                    || p.ConstraintTypes.Length != 0
                    || p.Variance != VarianceKind.None
                )
            )
                Report(
                    "shape",
                    definition,
                    "Declare an unconstrained public top-level IQuery interface with invariant type parameters and no base interfaces."
                );
            var members = definition.GetMembers();
            if (
                members.Length != 1
                || members[0] is not IMethodSymbol method
                || !ValidFetch(method, definition)
            )
                Report(
                    "fetch",
                    definition,
                    "Expose only Task<TOutput> Fetch(), or Task<TOutput> Fetch(TInput input), without Async, generic methods, optional parameters, or additional members."
                );
        }
        if (definitions.Any(d => d is null))
            return findings;
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
            .ToArray();
        foreach (var type in types)
        {
            if (definitions.Any(d => Same(type, d)))
                continue;
            var queries = type.AllInterfaces.Where(IsQuery).ToArray();
            if (type.TypeKind == TypeKind.Interface && queries.Length != 0)
            {
                Placement(type);
                if (
                    !PublicInterface(type)
                    || type.IsGenericType
                    || queries.Length != 1
                    || type.AllInterfaces.Append(type)
                        .Where(i => !IsQuery(i))
                        .Any(i => i.GetMembers().Length != 0)
                )
                    Report(
                        "single-query",
                        type,
                        "Declare a public nongeneric named port inheriting exactly one closed IQuery contract, with no additional members (including inherited members)."
                    );
                foreach (var query in queries)
                {
                    if (query.Arity == 2)
                        QueryDataInspection.Inspect(
                            query.TypeArguments[0],
                            applicationNamespace,
                            root,
                            false,
                            message => Report("input", type, message)
                        );
                    QueryDataInspection.Inspect(
                        query.TypeArguments[^1],
                        applicationNamespace,
                        root,
                        true,
                        message => Report("output", type, message)
                    );
                }
            }
            if (
                type.TypeKind != TypeKind.Class
                || type.IsAbstract
                || !type.AllInterfaces.Any(IsWorkflow)
            )
                continue;
            for (
                var current = type;
                current is not null;
                current = current.BaseType
            )
                foreach (
                    var parameter in current.InstanceConstructors.SelectMany(
                        c => c.Parameters
                    )
                )
                {
                    if (parameter.Type is not INamedTypeSymbol dependency)
                        continue;
                    if (
                        dependency.TypeKind != TypeKind.Interface
                        && dependency.AllInterfaces.Any(IsQuery)
                    )
                        Report(
                            "injection",
                            type,
                            $"Constructor dependency {parameter.Name} on {current.ToDisplayString()} uses concrete query implementation {dependency.ToDisplayString()}; inject a named query interface or IQuery instead."
                        );
                    // Bare closed generic contracts have no named declaration at which to check DTO placement.
                    if (IsQuery(dependency))
                    {
                        if (dependency.Arity == 2)
                            QueryDataInspection.Inspect(
                                dependency.TypeArguments[0],
                                applicationNamespace,
                                root,
                                false,
                                message => Report("input", type, message)
                            );
                        QueryDataInspection.Inspect(
                            dependency.TypeArguments[^1],
                            applicationNamespace,
                            root,
                            true,
                            message => Report("output", type, message)
                        );
                    }
                }
        }
        return findings.Distinct().ToArray();

        bool IsQuery(INamedTypeSymbol candidate) =>
            definitions.Any(d => Same(candidate.OriginalDefinition, d));
        bool IsWorkflow(INamedTypeSymbol candidate) =>
            candidate.ContainingNamespace.ToDisplayString()
                == contractNamespace + ".Contracts.Driving"
            && (
                (candidate.Name == "IUseCase" && candidate.Arity is 1 or 2)
                || (candidate.Name == "IQueryUseCase" && candidate.Arity == 2)
            );
        void Report(string rule, INamedTypeSymbol type, string message) =>
            findings.Add(
                new(
                    rule,
                    $"{type.ToDisplayString()} ({string.Join(", ", type.DeclaringSyntaxReferences.Select(r => Path.GetRelativePath(root, r.SyntaxTree.FilePath)))}): {message}"
                )
            );
        void Placement(INamedTypeSymbol type, string? expectedNamespace = null)
        {
            if (
                type.ContainingNamespace.ToDisplayString()
                    != (expectedNamespace ?? drivenNamespace)
                || type.ContainingType is not null
                || type.DeclaringSyntaxReferences.Length == 0
                || type.DeclaringSyntaxReferences.Any(r =>
                    Path.GetDirectoryName(
                            Path.GetRelativePath(root, r.SyntaxTree.FilePath)
                        )
                        ?.Replace('\\', '/') != "Contracts/Driven"
                )
            )
                Report(
                    "placement",
                    type,
                    $"Declare query ports directly in Contracts/Driven with namespace {drivenNamespace}."
                );
        }
        bool ValidFetch(IMethodSymbol method, INamedTypeSymbol definition) =>
            method.Name == "Fetch"
            && method.MethodKind == MethodKind.Ordinary
            && method.DeclaredAccessibility == Accessibility.Public
            && method.IsAbstract
            && !method.IsStatic
            && method.Arity == 0
            && !method.ReturnsByRef
            && !method.ReturnsByRefReadonly
            && method.Parameters.Length == definition.Arity - 1
            && (
                method.Parameters.Length == 0
                || (
                    Same(
                        method.Parameters[0].Type,
                        definition.TypeParameters[0]
                    )
                    && method.Parameters[0].RefKind == RefKind.None
                    && !method.Parameters[0].IsOptional
                    && !method.Parameters[0].IsParams
                )
            )
            && method.ReturnType is INamedTypeSymbol result
            && Same(
                result.OriginalDefinition,
                compilation.GetTypeByMetadataName(
                    "System.Threading.Tasks.Task`1"
                )
            )
            && Same(result.TypeArguments[0], definition.TypeParameters[^1]);
    }

    private static bool PublicInterface(INamedTypeSymbol type) =>
        type.TypeKind == TypeKind.Interface
        && type.DeclaredAccessibility == Accessibility.Public
        && type.ContainingType is null;

    private static bool Same(ISymbol? left, ISymbol? right) =>
        SymbolEqualityComparer.Default.Equals(left, right);
}
