using Microsoft.CodeAnalysis;

namespace Company.BoundedContext.DesignTests.Common;

internal static class QueryDataInspection
{
    public static void Inspect(
        ITypeSymbol type,
        string applicationNamespace,
        string root,
        bool allowCollections,
        Action<string> report
    )
    {
        Check(type, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default));
        string[] Paths(ITypeSymbol candidate) =>
            candidate
                .DeclaringSyntaxReferences.Select(reference =>
                    Path.GetRelativePath(root, reference.SyntaxTree.FilePath)
                        .Replace('\\', '/')
                )
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        void Check(ITypeSymbol output, HashSet<ITypeSymbol> seen)
        {
            if (!seen.Add(output))
                return;
            if (allowCollections && output is IArrayTypeSymbol array)
            {
                Check(array.ElementType, seen);
                return;
            }
            if (
                output.TypeKind == TypeKind.Enum
                || output.SpecialType
                    is SpecialType.System_Boolean
                        or SpecialType.System_Char
                        or SpecialType.System_SByte
                        or SpecialType.System_Byte
                        or SpecialType.System_Int16
                        or SpecialType.System_UInt16
                        or SpecialType.System_Int32
                        or SpecialType.System_UInt32
                        or SpecialType.System_Int64
                        or SpecialType.System_UInt64
                        or SpecialType.System_Decimal
                        or SpecialType.System_Single
                        or SpecialType.System_Double
                        or SpecialType.System_IntPtr
                        or SpecialType.System_UIntPtr
                        or SpecialType.System_String
            )
                return;
            if (
                output.ContainingNamespace?.ToDisplayString() == "System"
                && output.Name
                    is "Guid"
                        or "DateTime"
                        or "DateTimeOffset"
                        or "DateOnly"
                        or "TimeOnly"
                        or "TimeSpan"
                        or "Uri"
                        or "Version"
            )
                return;
            if (
                output is INamedTypeSymbol named
                && (
                    named.OriginalDefinition.SpecialType
                        == SpecialType.System_Nullable_T
                    || (
                        allowCollections
                        && named.ContainingAssembly.Name.StartsWith(
                            "System",
                            StringComparison.Ordinal
                        )
                        && named
                            .ContainingNamespace.ToDisplayString()
                            .StartsWith(
                                "System.Collections",
                                StringComparison.Ordinal
                            )
                        && named.IsGenericType
                        && (
                            named.Name == "IEnumerable"
                            || named.AllInterfaces.Any(i =>
                                i.SpecialType
                                == SpecialType.System_Collections_IEnumerable
                            )
                        )
                    )
                )
            )
            {
                foreach (var argument in named.TypeArguments)
                    Check(argument, seen);
                return;
            }
            if (
                output.TypeKind is not (TypeKind.Class or TypeKind.Struct)
                || output.ContainingNamespace?.ToDisplayString()
                    != applicationNamespace + ".Contracts.Data"
                || output.ContainingType is not null
                || output.DeclaringSyntaxReferences.Length == 0
                || Paths(output)
                    .Any(path =>
                        Path.GetDirectoryName(path)?.Replace('\\', '/')
                        != "Contracts/Data"
                    )
            )
                report(
                    $"Declare custom query DTO {output.ToDisplayString()} directly in Contracts/Data with namespace {applicationNamespace}.Contracts.Data; use scalar values or Contracts/Data DTOs (standard collections are allowed for results)."
                );
        }
    }
}
