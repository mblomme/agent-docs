using System.Reflection;
using Company.BoundedContext.DesignTests.Common;

namespace Company.BoundedContext.DesignTests;

public class ToolkitLibraryTests
{
    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    public void ReusableContractsBelongToReferencedToolkitLibraries(
        string layer
    )
    {
        var production = DomainTestModel.Production;
        var product = ProductAssembly(production, layer);
        var findings = ProjectFindings(product, layer);
        findings.AddRange(DuplicateContractFindings(product, layer));
        findings.AddRange(ToolkitAssemblyFindings(production, layer));

        Scenario
            .Given($"the {layer} product and toolkit projects")
            .When(findings)
            .ThenNoViolations(
                "Keep common contracts in the toolkit library and product-specific types in the product layers."
            );
    }

    private static Assembly ProductAssembly(
        Assembly[] production,
        string layer
    ) =>
        layer == "Domain"
            ? AssemblyDiscovery.DomainAssembly(production)
            : AssemblyDiscovery.ApplicationAssembly(production);

    private static List<string> ProjectFindings(Assembly product, string layer)
    {
        string? project = null;
        var findings = DomainProjectInspection
            .Project(
                AppContext.BaseDirectory,
                typeof(ToolkitLibraryTests).Assembly.GetName().Name!,
                product.GetName().Name!,
                path => project = path,
                layer
            )
            .ToList();

        if (project is null)
            return findings;

        findings.AddRange(ToolkitProjectInspection.Inspect(project, layer));
        if (
            layer == "Domain"
            && Directory.Exists(
                Path.Combine(Path.GetDirectoryName(project)!, "Shared")
            )
        )
            findings.Add(
                "Domain/Shared is forbidden. Move common abstractions to lib/DddToolkit.Domain and remove Shared/. Create the folder lib if it does not exist, then create the project lib/DddToolkit.Domain and reference it from the product domain project."
            );

        return findings;
    }

    private static IEnumerable<string> DuplicateContractFindings(
        Assembly product,
        string layer
    )
    {
        var contracts =
            layer == "Domain"
                ? ToolkitProjectInspection.DomainContracts
                : ToolkitProjectInspection.ApplicationContracts;
        return product
            .GetTypes()
            .Where(type => contracts.Contains(type.Name))
            .Select(type =>
                $"{type.FullName}: duplicate common contract in product {layer}. Move it to DddToolkit.{layer}. Create the lib folder and the project lib/DddToolkit.{layer} if it does not exist."
            );
    }

    private static IEnumerable<string> ToolkitAssemblyFindings(
        Assembly[] production,
        string layer
    )
    {
        var toolkits = production
            .Where(assembly => assembly.GetName().Name == "DddToolkit." + layer)
            .ToArray();
        if (toolkits.Length != 1)
            return
            [
                $"Expected exactly one DddToolkit.{layer} assembly; found {toolkits.Length}. Create and reference lib/DddToolkit.{layer}/DddToolkit.{layer}.csproj, then rebuild.",
            ];

        var productNames = production
            .Where(assembly =>
                !assembly
                    .GetName()
                    .Name!.StartsWith("DddToolkit.", StringComparison.Ordinal)
            )
            .Select(assembly => assembly.GetName().Name)
            .ToHashSet(StringComparer.Ordinal);
        return toolkits[0]
            .GetReferencedAssemblies()
            .Where(reference =>
                productNames.Contains(reference.Name)
                || layer == "Domain"
                    && reference.Name == "DddToolkit.Application"
            )
            .Select(reference =>
                $"DddToolkit.{layer}: forbidden dependency on {reference.Name}; toolkit libraries must be project agnostic."
            );
    }
}
