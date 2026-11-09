namespace Company.BoundedContext.DesignTests.Common;

public class AggregateCreationRegressionTests
{
    [Fact]
    public void PrivateConstructorsAndDomainNamedFactoryAreAccepted()
    {
        Assert.Empty(
            AggregateCreationInspection.ParameterlessConstructor(typeof(Valid))
        );
        Assert.Empty(
            AggregateCreationInspection.PrivateConstructors(typeof(Valid))
        );
        var example = Example<Valid>("Capture");
        Assert.Empty(AggregateCreationInspection.FactoryContract(example));
        Assert.Empty(AggregateCreationInspection.FreshInstances(example));
    }

    [Theory]
    [InlineData(typeof(PublicConstructor))]
    [InlineData(typeof(InternalConstructor))]
    [InlineData(typeof(ProtectedConstructor))]
    [InlineData(typeof(NoParameterlessConstructor))]
    public void MissingPrivateParameterlessConstructorIsRejected(Type root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Assert.Contains(
            "private parameterless",
            Assert.Single(
                AggregateCreationInspection.ParameterlessConstructor(root)
            ),
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(typeof(PublicConstructor))]
    [InlineData(typeof(InternalConstructor))]
    [InlineData(typeof(ProtectedConstructor))]
    [InlineData(typeof(MixedConstructors))]
    public void EveryNonPrivateConstructorIsRejected(Type root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Assert.Contains(
            "make this instance constructor private",
            Assert.Single(
                AggregateCreationInspection.PrivateConstructors(root)
            ),
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(typeof(MissingFactory))]
    [InlineData(typeof(InstanceFactory))]
    [InlineData(typeof(HiddenFactory))]
    [InlineData(typeof(WrongReturnFactory))]
    [InlineData(typeof(GenericFactory))]
    [InlineData(typeof(InheritedFactory))]
    public void InvalidFactorySignaturesAreRejected(Type root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Assert.Contains(
            "public static non-generic",
            Assert.Single(
                AggregateCreationInspection.FactoryContract(
                    new(root, "Create", () => [])
                )
            ),
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(typeof(NullFactory))]
    [InlineData(typeof(CachedFactory))]
    public void NullOrReusedInstancesAreRejected(Type root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Assert.Contains(
            "non-null new instance",
            Assert.Single(
                AggregateCreationInspection.FreshInstances(
                    new(root, "Create", () => [])
                )
            ),
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(typeof(ThrowingFactory))]
    [InlineData(typeof(RequiresArguments))]
    public void InvocationErrorsExplainHowToRepairFactoryOrExamples(Type root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Assert.Contains(
            "configured valid arguments",
            Assert.Single(
                AggregateCreationInspection.FreshInstances(
                    new(root, "Create", () => [])
                )
            ),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ProductionDiscoveryIncludesNewConcreteRootsAndExcludesAbstractRoots()
    {
        var roots = DomainEventContractInspection.AggregateRoots(
            typeof(AggregateCreationRegressionTests).Assembly,
            typeof(FixtureRoot<>)
        );
        Assert.Contains(typeof(Discovered), roots);
        Assert.DoesNotContain(typeof(AbstractRoot), roots);
    }

    private static AggregateCreationExample Example<T>(string name) =>
        new(typeof(T), name, () => []);

    // These declarations intentionally model invalid construction contracts and are inspected via reflection.
#pragma warning disable CA1812, CA1822, CA1852, CA1859
    private sealed class Valid
    {
        private Valid() { }

        private Valid(int value) { }

        public static Valid Capture() => new();
    }

    private sealed class PublicConstructor
    {
        public PublicConstructor() { }
    }

    private sealed class InternalConstructor
    {
        internal InternalConstructor() { }
    }

    private class ProtectedConstructor
    {
        protected ProtectedConstructor() { }
    }

    private sealed class NoParameterlessConstructor
    {
        private NoParameterlessConstructor(int value) { }
    }

    private sealed class MixedConstructors
    {
        private MixedConstructors() { }

        public MixedConstructors(int value) { }
    }

    private sealed class MissingFactory;

    private sealed class InstanceFactory
    {
        public InstanceFactory Create() => new();
    }

    private sealed class HiddenFactory
    {
        private static HiddenFactory Create() => new();
    }

    private sealed class WrongReturnFactory
    {
        public static object Create() => new WrongReturnFactory();
    }

    private sealed class GenericFactory
    {
        public static GenericFactory Create<T>() => new();
    }

    private class FactoryBase
    {
        public static FactoryBase Create() => new();
    }

    private sealed class InheritedFactory : FactoryBase;

    private sealed class NullFactory
    {
        public static NullFactory? Create() => null;
    }

    private sealed class CachedFactory
    {
        private static readonly CachedFactory Instance = new();

        public static CachedFactory Create() => Instance;
    }

    private sealed class ThrowingFactory
    {
        public static ThrowingFactory Create() =>
            throw new InvalidOperationException("Invalid creation.");
    }

    private sealed class RequiresArguments
    {
        public static RequiresArguments Create(int value) => new();
    }

    private abstract class FixtureRoot<T>;

    private abstract class AbstractRoot : FixtureRoot<Guid>;

    private sealed class Discovered : AbstractRoot;
#pragma warning restore CA1812, CA1822, CA1852, CA1859
}
