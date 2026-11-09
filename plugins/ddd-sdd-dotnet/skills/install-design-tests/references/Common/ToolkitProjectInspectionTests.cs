namespace Company.BoundedContext.DesignTests.Common;

public sealed class ToolkitProjectInspectionTests : IDisposable
{
    private readonly string checkout = Path.Combine(
        Path.GetTempPath(),
        "toolkit-project-" + Guid.NewGuid().ToString("N")
    );

    [Theory]
    [InlineData("/")]
    [InlineData("\\")]
    public void ToolkitIsResolvedThroughTransitiveReferences(string separator)
    {
        var product = CreateCheckout(separator);
        Assert.Empty(
            ToolkitProjectInspection.Inspect(product, "Domain", "Entity.cs")
        );
        string? found = null;
        Assert.Empty(
            DomainProjectInspection.Project(
                Output(),
                "Rules",
                "DddToolkit.Domain",
                path => found = path
            )
        );
        Assert.Equal(Toolkit(), found);
    }

    [Fact]
    public void MissingToolkitHasCreationGuidance()
    {
        var product = CreateCheckout();
        File.Delete(Toolkit());
        Assert.Contains(
            ToolkitProjectInspection.Inspect(product, "Domain"),
            message =>
                message.Contains(
                    "Create the DddToolkit.Domain",
                    StringComparison.Ordinal
                )
        );
    }

    [Fact]
    public void ExistingButUnreferencedToolkitFails()
    {
        var product = CreateCheckout();
        File.WriteAllText(product, "<Project />");
        Assert.Contains(
            ToolkitProjectInspection.Inspect(product, "Domain", "Entity.cs"),
            message =>
                message.Contains(
                    "missing project reference",
                    StringComparison.Ordinal
                )
        );
    }

    [Theory]
    [InlineData("Nested/Entity.cs", "DddToolkit.Domain")]
    [InlineData("Entity.cs", "Product.Domain.Shared")]
    public void NamespaceAndPhysicalPlacementAreBothRequired(
        string path,
        string ns
    )
    {
        var product = CreateCheckout();
        File.Delete(
            Path.Combine(Path.GetDirectoryName(Toolkit())!, "Entity.cs")
        );
        Write(
            Path.Combine(Path.GetDirectoryName(Toolkit())!, path),
            $"namespace {ns}; public abstract class Entity<T>; "
        );
        Assert.Contains(
            ToolkitProjectInspection.Inspect(product, "Domain", "Entity.cs"),
            message =>
                message.Contains(
                    "place the source declaration",
                    StringComparison.Ordinal
                )
        );
    }

    [Fact]
    public void MissingAndDuplicateDeclarationsFail()
    {
        var product = CreateCheckout();
        var entity = Path.Combine(
            Path.GetDirectoryName(Toolkit())!,
            "Entity.cs"
        );
        File.Delete(entity);
        Assert.Contains(
            ToolkitProjectInspection.Inspect(product, "Domain", "Entity.cs"),
            message => message.Contains("found 0", StringComparison.Ordinal)
        );
        File.WriteAllText(
            entity,
            "namespace DddToolkit.Domain; public abstract class Entity<T>; public abstract class Entity<T>; "
        );
        Assert.Contains(
            ToolkitProjectInspection.Inspect(product, "Domain", "Entity.cs"),
            message => message.Contains("found 2", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void SharedFolderIsForbiddenEvenWhenEmpty()
    {
        CreateCheckout();
        Directory.CreateDirectory(
            Path.Combine(checkout, "src", "Product.Domain", "Shared")
        );
        Assert.Contains(
            DomainProjectInspection.ToolkitSources(
                Output(),
                "Rules",
                "Product.Domain",
                "Entity.cs"
            ),
            message =>
                message.Contains(
                    "Domain/Shared is forbidden",
                    StringComparison.Ordinal
                )
        );
    }

    [Theory]
    [InlineData("Domain", "Product.Domain", false)]
    [InlineData("Domain", "DddToolkit.Application", false)]
    [InlineData("Application", "DddToolkit.Domain", true)]
    public void OnlyApplicationToDomainToolkitDependencyIsAllowed(
        string layer,
        string dependency,
        bool allowed
    )
    {
        var product = CreateCheckout();
        var library = Path.Combine(
            checkout,
            "lib",
            "DddToolkit." + layer,
            "DddToolkit." + layer + ".csproj"
        );
        var target =
            dependency == "Product.Domain"
                ? product
                : Path.Combine(
                    checkout,
                    "lib",
                    dependency,
                    dependency + ".csproj"
                );
        if (!File.Exists(target))
            Write(target, "<Project />");
        Write(
            library,
            $"<Project><ItemGroup><ProjectReference Include=\"{Path.GetRelativePath(Path.GetDirectoryName(library)!, target)}\" /></ItemGroup></Project>"
        );
        // Link the product to the selected library without creating a cycle through itself.
        if (layer == "Application")
            File.WriteAllText(
                product,
                $"<Project><ItemGroup><ProjectReference Include=\"{Path.GetRelativePath(Path.GetDirectoryName(product)!, library)}\" /></ItemGroup></Project>"
            );
        var violations = ToolkitProjectInspection
            .Inspect(product, layer)
            .ToArray();
        Assert.Equal(
            !allowed,
            violations.Any(message =>
                message.Contains(
                    "forbidden project dependency",
                    StringComparison.Ordinal
                )
            )
        );
    }

    [Fact]
    public void MalformedProjectAndMissingCheckoutGiveDiagnostics()
    {
        var product = CreateCheckout();
        File.WriteAllText(Toolkit(), "<Project>");
        Assert.Contains(
            ToolkitProjectInspection.Inspect(product, "Domain"),
            message =>
                message.Contains(
                    "Repair the project XML",
                    StringComparison.Ordinal
                )
        );
        Assert.Contains(
            DomainProjectInspection.Project(
                checkout,
                "Absent",
                "Product.Domain",
                _ => { }
            ),
            message =>
                message.Contains("source checkout", StringComparison.Ordinal)
        );
    }

    [Theory]
    [InlineData(
        "Contracts/Driving/Ports.cs",
        "DddToolkit.Application.Contracts.Driving",
        true
    )]
    [InlineData(
        "Contracts/Driven/Ports.cs",
        "DddToolkit.Application.Contracts.Driving",
        false
    )]
    [InlineData(
        "Contracts/Driving/Ports.cs",
        "Product.Application.Contracts.Driving",
        false
    )]
    public void CommonApplicationPortsUseToolkitContractFolders(
        string path,
        string ns,
        bool valid
    )
    {
        var library = Path.Combine(
            checkout,
            "lib",
            "DddToolkit.Application",
            "DddToolkit.Application.csproj"
        );
        var product = Path.Combine(
            checkout,
            "src",
            "Product.Application",
            "Product.Application.csproj"
        );
        Write(library, "<Project />");
        Write(
            product,
            $"<Project><ItemGroup><ProjectReference Include=\"{Path.GetRelativePath(Path.GetDirectoryName(product)!, library)}\" /></ItemGroup></Project>"
        );
        Write(
            Path.Combine(Path.GetDirectoryName(library)!, path),
            $"namespace {ns}; public interface IUseCase<T>; public interface IUseCase<T, R>;"
        );
        var findings = ToolkitProjectInspection
            .Inspect(product, "Application", "IUseCase.cs")
            .ToArray();
        Assert.Equal(valid, findings.Length == 0);
        if (!valid)
            Assert.All(
                findings,
                message =>
                    Assert.Contains(
                        "place the source declaration",
                        message,
                        StringComparison.Ordinal
                    )
            );
    }

    [Fact]
    public void BrokenToolkitReferencesAreReported()
    {
        var product = CreateCheckout();
        File.WriteAllText(
            Toolkit(),
            "<Project><ItemGroup><ProjectReference Include=\"../Missing/Missing.csproj\" /></ItemGroup></Project>"
        );
        Assert.Contains(
            ToolkitProjectInspection.Inspect(product, "Domain", "Entity.cs"),
            message =>
                message.Contains(
                    "broken ProjectReference",
                    StringComparison.Ordinal
                )
        );
    }

    private string CreateCheckout(string separator = "/")
    {
        var product = Path.Combine(
            checkout,
            "src",
            "Product.Domain",
            "Product.Domain.csproj"
        );
        var reference = Path.GetRelativePath(
                Path.GetDirectoryName(product)!,
                Toolkit()
            )
            .Replace(
                Path.DirectorySeparatorChar.ToString(),
                separator,
                StringComparison.Ordinal
            );
        Write(
            product,
            $"<Project><ItemGroup><ProjectReference Include=\"{reference}\" /></ItemGroup></Project>"
        );
        Write(Toolkit(), "<Project />");
        Write(
            Path.Combine(Path.GetDirectoryName(Toolkit())!, "Entity.cs"),
            "namespace DddToolkit.Domain; public abstract class Entity<T>;"
        );
        var test = Path.Combine(checkout, "tests", "Rules", "Rules.csproj");
        Write(
            test,
            $"<Project><ItemGroup><ProjectReference Include=\"{Path.GetRelativePath(Path.GetDirectoryName(test)!, product)}\" /></ItemGroup></Project>"
        );
        Directory.CreateDirectory(Output());
        return product;
    }

    private string Toolkit() =>
        Path.Combine(
            checkout,
            "lib",
            "DddToolkit.Domain",
            "DddToolkit.Domain.csproj"
        );

    private string Output() =>
        Path.Combine(checkout, "tests", "Rules", "bin", "Debug", "net10.0");

    private static void Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(checkout))
            Directory.Delete(checkout, true);
        GC.SuppressFinalize(this);
    }
}
