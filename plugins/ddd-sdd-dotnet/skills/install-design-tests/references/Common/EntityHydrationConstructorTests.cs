namespace Company.BoundedContext.DesignTests.Common;

public class EntityHydrationConstructorTests
{
    [Theory]
    [InlineData(typeof(PrivateEntity))]
    [InlineData(typeof(ProtectedBase<>))]
    [InlineData(typeof(IntermediateBase))]
    [InlineData(typeof(ImplicitProtectedBase))]
    public void CorrectConstructorsAreAccepted(Type entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Assert.Empty(EntityContractInspection.HydrationConstructor(entity));
    }

    [Theory]
    [InlineData(typeof(PublicEntity), "private")]
    [InlineData(typeof(InternalEntity), "private")]
    [InlineData(typeof(ProtectedEntity), "private")]
    [InlineData(typeof(MissingEntity), "private")]
    [InlineData(typeof(OptionalEntity), "private")]
    [InlineData(typeof(ImplicitPublicEntity), "private")]
    [InlineData(typeof(PrivateBase), "protected")]
    [InlineData(typeof(PublicBase), "protected")]
    [InlineData(typeof(InternalBase), "protected")]
    [InlineData(typeof(ProtectedInternalBase), "protected")]
    [InlineData(typeof(PrivateProtectedBase), "protected")]
    [InlineData(typeof(MissingBase), "protected")]
    public void MissingOrIncorrectAccessibilityIsRejected(
        Type entity,
        string required
    )
    {
        ArgumentNullException.ThrowIfNull(entity);
        var message = Assert.Single(
            EntityContractInspection.HydrationConstructor(entity)
        );
        Assert.Contains(entity.FullName!, message, StringComparison.Ordinal);
        Assert.Contains(
            required + " parameterless constructor",
            message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void DiscoveryIncludesOpenGenericAndIntermediateBasesAndConcreteDescendants()
    {
        var types = EntityContractInspection.EntityTypes(
            typeof(EntityHydrationConstructorTests).Assembly,
            typeof(ProtectedBase<>)
        );
        Assert.Contains(typeof(ProtectedBase<>), types);
        Assert.Contains(typeof(IntermediateBase), types);
        Assert.Contains(typeof(PrivateEntity), types);
        Assert.DoesNotContain(typeof(Unrelated), types);
    }

    // Deliberately invalid reflection fixtures exercise constructor access rules.
#pragma warning disable CA1812, CA1852
    private abstract class ProtectedBase<T>
    {
        protected ProtectedBase() { }

        protected ProtectedBase(int value) { }
    }

    private abstract class IntermediateBase : ProtectedBase<Guid>
    {
        protected IntermediateBase() { }
    }

    private sealed class PrivateEntity : IntermediateBase
    {
        private PrivateEntity() { }

        internal PrivateEntity(int value) { }
    }

    private class PublicEntity
    {
        public PublicEntity() { }
    }

    private class InternalEntity
    {
        internal InternalEntity() { }
    }

    private class ProtectedEntity
    {
        protected ProtectedEntity() { }
    }

    private sealed class MissingEntity : ProtectedBase<Guid>
    {
        private MissingEntity(int value)
            : base(value) { }
    }

    private sealed class OptionalEntity
    {
        private OptionalEntity(int value = 0) { }
    }

    private class ImplicitPublicEntity;

    private abstract class ImplicitProtectedBase;

    private abstract class PrivateBase
    {
        private PrivateBase() { }
    }

    private abstract class PublicBase
    {
        public PublicBase() { }
    }

    private abstract class InternalBase
    {
        internal InternalBase() { }
    }

    private abstract class ProtectedInternalBase
    {
        protected internal ProtectedInternalBase() { }
    }

    private abstract class PrivateProtectedBase
    {
        private protected PrivateProtectedBase() { }
    }

    // This fixture deliberately has only a protected, parameterized constructor.
#pragma warning disable IDE0290
    private abstract class MissingBase
    {
        protected MissingBase(int value) { }
    }
#pragma warning restore IDE0290

    private sealed class Unrelated;
#pragma warning restore CA1812, CA1852
}
