namespace Company.BoundedContext.DesignTests.Common;

public class DomainTypeSourceInspectionTests
{
    [Theory]
    [InlineData("Shared/Amount.cs", false)]
    [InlineData("Shared/Values/Amount.cs", false)]
    [InlineData("Shared\\Values\\Amount.cs", false)]
    [InlineData("Orders/ValueObjects/Amount.cs", true)]
    [InlineData("SharedValues/Amount.cs", true)]
    public void ConcreteValueObjectPlacementChecksPhysicalSharedFolder(
        string path,
        bool valid
    )
    {
        var violations = DomainTypeSourceInspection
            .ConcreteValueObjectsOutsideShared(
                ["Example.Amount"],
                [(path, "namespace Example; sealed record Amount;")]
            )
            .ToArray();
        if (valid)
            Assert.Empty(violations);
        else
        {
            var message = Assert.Single(violations);
            Assert.Contains(path, message, StringComparison.Ordinal);
            Assert.Contains(
                "Example.Amount",
                message,
                StringComparison.Ordinal
            );
            Assert.Contains(
                "Carefully read the domain-layer specification again for correct folder locations",
                message,
                StringComparison.Ordinal
            );
        }
    }

    [Fact]
    public void ValueObjectPlacementOnlyInspectsConcreteTypes() =>
        Assert.Empty(
            DomainTypeSourceInspection.ConcreteValueObjectsOutsideShared(
                ["Example.Amount"],
                [
                    (
                        "../lib/DddToolkit.Domain/ValueObject.cs",
                        "namespace DddToolkit.Domain; abstract record ValueObject;"
                    ),
                    (
                        "Orders/ValueObjects/Amount.cs",
                        "namespace Example; sealed record Amount : DddToolkit.Domain.ValueObject;"
                    ),
                ]
            )
        );

    [Fact]
    public void MissingConcreteValueObjectSourceCannotSilentlyPass() =>
        Assert.Contains(
            "cannot locate its source declaration",
            Assert.Single(
                DomainTypeSourceInspection.ConcreteValueObjectsOutsideShared(
                    ["Example.Amount"],
                    []
                )
            ),
            StringComparison.Ordinal
        );

    [Theory]
    [InlineData("namespace Example; class Owner { } record OwnerId;", true)]
    [InlineData(
        "namespace Example { class Owner { } readonly record struct OwnerId(int Value); }",
        true
    )]
    [InlineData("namespace Example; class Owner { } // record OwnerId;", false)]
    [InlineData(
        "namespace Example; class Owner { } namespace Other { record OwnerId; }",
        false
    )]
    public void IdentityPlacementUsesDeclarationsInTheOwnersFile(
        string source,
        bool valid
    )
    {
        var violations = DomainTypeSourceInspection
            .IdentityInSameFile(
                [("Example.Owner", "Example.OwnerId")],
                [
                    ("Owner.cs", source),
                    ("OwnerId.cs", "namespace Example; record OwnerId;"),
                ]
            )
            .ToArray();
        if (valid)
            Assert.Empty(violations);
        else
        {
            var message = Assert.Single(violations);
            Assert.Contains(
                "Example.OwnerId",
                message,
                StringComparison.Ordinal
            );
            Assert.Contains("Owner.cs", message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MissingOwnerSourceFailsIdentityPlacement() =>
        Assert.Contains(
            "cannot locate",
            Assert.Single(
                DomainTypeSourceInspection.IdentityInSameFile(
                    [("Example.Owner", "Example.OwnerId")],
                    []
                )
            ),
            StringComparison.Ordinal
        );

    [Theory]
    [InlineData("domain event")]
    [InlineData("value object")]
    public void BundledEmptyRecordsAndClassesAreRejected(string kind)
    {
        var message = Assert.Single(
            DomainTypeSourceInspection.OnePerFile(
                ["Example.First", "Example.Second"],
                [
                    (
                        "Bundled.cs",
                        "namespace Example; public sealed record First; public sealed class Second { }"
                    ),
                ],
                kind
            )
        );
        Assert.Contains("Bundled.cs", message, StringComparison.Ordinal);
        Assert.Contains(
            "Example.First, Example.Second",
            message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "separate source file",
            message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void SeparateFilesWithAbstractBasesAndUnrelatedTypesAreAccepted() =>
        Assert.Empty(
            DomainTypeSourceInspection.OnePerFile(
                ["Example.First", "Example.Second"],
                [
                    (
                        "First.cs",
                        "namespace Example; abstract record Base; sealed record First : Base; class Helper { }"
                    ),
                    (
                        "Second.cs",
                        "namespace Example { readonly record struct Second(int Value); }"
                    ),
                ],
                "value object"
            )
        );

    [Fact]
    public void NamespaceAndNestedGenericIdentityAreRespected() =>
        Assert.Empty(
            DomainTypeSourceInspection.OnePerFile(
                ["Example.Outer`1+Inner", "Other.Inner"],
                [
                    (
                        "Nested.cs",
                        "namespace Example { class Outer<T> { record Inner; } }"
                    ),
                    (
                        "Other.cs",
                        "namespace Other; record Inner; // namespace Example; record Fake;"
                    ),
                ],
                "domain event"
            )
        );

    [Fact]
    public void MultiplePartialDeclarationsOfOneTypeDoNotCountTwice() =>
        Assert.Empty(
            DomainTypeSourceInspection.OnePerFile(
                ["Example.First"],
                [
                    (
                        "First.cs",
                        "namespace Example; partial record First; partial record First { }"
                    ),
                ],
                "value object"
            )
        );

    [Fact]
    public void MissingSourceForConcreteTypeFailsWithGuidance()
    {
        Assert.Contains(
            "cannot locate its source declaration",
            Assert.Single(
                DomainTypeSourceInspection.OnePerFile(
                    ["Example.Missing"],
                    [],
                    "domain event"
                )
            ),
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData("value object")]
    [InlineData("domain event")]
    public void EmptyDiscoveryDoesNotRequireSourceFilesOrProject(string kind)
    {
        Assert.Empty(DomainTypeSourceInspection.OnePerFile([], [], kind));
        Assert.Empty(
            DomainTypeSourceInspection.OnePerFile(
                "missing-output",
                "missing-tests",
                "missing-domain",
                [],
                kind
            )
        );
    }

    [Fact]
    public void EmptyOwnerDiscoveryDoesNotRequireSourceFilesOrProject()
    {
        Assert.Empty(DomainTypeSourceInspection.IdentityInSameFile([], []));
        Assert.Empty(
            DomainTypeSourceInspection.IdentityInSameFile(
                "missing-output",
                "missing-tests",
                "missing-domain",
                [],
                typeof(object)
            )
        );
    }

    [Fact]
    public void MatchingFilenamesDoNotHideBundling() =>
        Assert.Single(
            DomainTypeSourceInspection.OnePerFile(
                ["Example.First", "Example.Second"],
                [
                    (
                        "First.cs",
                        "namespace Example; record First; record Second;"
                    ),
                    ("Second.cs", "// Placeholder is not a declaration."),
                ],
                "value object"
            )
        );
}
