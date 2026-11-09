namespace Company.BoundedContext.DesignTests.Common;

public sealed class DomainProjectInspectionTests : IDisposable
{
    private readonly string checkout = Path.Combine(
        Path.GetTempPath(),
        "ddd-project-" + Guid.NewGuid().ToString("N")
    );

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void AggregateSourcesMustShareTheConventionalFolder(
        bool assertionBesideRoot,
        bool rootExists
    )
    {
        var output = CreateCheckout();
        var domain = Path.Combine(checkout, "src", "Business");
        var folder = Path.Combine(domain, "Orders");
        Directory.CreateDirectory(folder);
        if (rootExists)
            File.WriteAllText(
                Path.Combine(folder, "Order.cs"),
                "namespace Example.Domain.Orders; class Order;"
            );
        File.WriteAllText(
            Path.Combine(
                assertionBesideRoot ? folder : domain,
                "OrderAsserts.cs"
            ),
            "namespace Example.Domain.Orders; class OrderAsserts;"
        );
        var violations = DomainProjectInspection
            .AggregateAssertions(
                output,
                "Rules",
                "Example.Domain",
                [typeof(Example.Domain.Orders.Order)]
            )
            .ToArray();
        if (assertionBesideRoot && rootExists)
            Assert.Empty(violations);
        else
        {
            var message = Assert.Single(violations);
            Assert.Contains(
                "Place Order.cs and OrderAsserts.cs together",
                message,
                StringComparison.Ordinal
            );
        }
    }

    [Fact]
    public void AggregatePlacementReportsMissingCheckout()
    {
        Directory.CreateDirectory(checkout);
        var message = Assert.Single(
            DomainProjectInspection.AggregateAssertions(
                checkout,
                "Rules",
                "Example.Domain",
                [typeof(Example.Domain.Orders.Order)]
            )
        );
        Assert.Contains("source checkout", message, StringComparison.Ordinal);
    }

    private string CreateCheckout()
    {
        var domain = Path.Combine(checkout, "src", "Business");
        var tests = Path.Combine(checkout, "tests", "Rules");
        var output = Path.Combine(tests, "bin", "Debug", "net10.0");
        Directory.CreateDirectory(domain);
        Directory.CreateDirectory(output);
        // AssemblyName differs from the filename to exercise real project metadata.
        File.WriteAllText(
            Path.Combine(domain, "Business.csproj"),
            "<Project><PropertyGroup><AssemblyName>Example.Domain</AssemblyName></PropertyGroup></Project>"
        );
        var reference = string.Join(
            "/",
            "..",
            "..",
            "src",
            "Business",
            "Business.csproj"
        );
        File.WriteAllText(
            Path.Combine(tests, "Rules.csproj"),
            $"<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><ItemGroup><ProjectReference Include=\"{reference}\" /></ItemGroup></Project>"
        );
        return output;
    }

    public void Dispose()
    {
        if (Directory.Exists(checkout))
            Directory.Delete(checkout, true);
        GC.SuppressFinalize(this);
    }
}
