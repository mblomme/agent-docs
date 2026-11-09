namespace Company.BoundedContext.DesignTests.Common;

public class EntityContractRegressionTests
{
    [Fact]
    public void ValidFactoryAndEquatableEntityContractsAreAccepted()
    {
        Assert.Empty(
            EntityContractInspection.IdFactoryClass(typeof(ValidFactory))
        );
        Assert.Empty(
            EntityContractInspection.IdFactoryClass(typeof(RegularFactory))
        );
        Assert.Empty(
            EntityContractInspection.IdFactoryMethod(typeof(ValidFactory))
        );
        Assert.Empty(
            EntityContractInspection.IdFactoryMethod(typeof(RegularFactory))
        );
        Assert.Empty(EntityContractInspection.BaseClass(typeof(ValidEntity<>)));
        Assert.Empty(EntityContractInspection.Equatable(typeof(ValidEntity<>)));
    }

    [Theory]
    [InlineData(typeof(IInvalidFactory))]
    [InlineData(typeof(GenericFactory<>))]
    [InlineData(typeof(HiddenFactory))]
    public void FactoryClassRejectsWrongKindGenericArityAndVisibility(
        Type factory
    )
    {
        ArgumentNullException.ThrowIfNull(factory);
        Assert.NotEmpty(EntityContractInspection.IdFactoryClass(factory));
    }

    [Theory]
    [InlineData(typeof(MissingFactory))]
    [InlineData(typeof(InstanceFactory))]
    [InlineData(typeof(ProtectedFactory))]
    [InlineData(typeof(WrongInputFactory))]
    [InlineData(typeof(WrongReturnFactory))]
    [InlineData(typeof(ParameterlessFactory))]
    [InlineData(typeof(NonGenericFactory))]
    [InlineData(typeof(TwoParametersFactory))]
    public void FactoryRejectsMissingOrIncorrectCreateSignatures(Type factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Entity ID factory method",
                EntityContractInspection.IdFactoryMethod(factory),
                "Add public static TId Create<TId>(Guid value) to EntityId."
            )
        );
        Assert.Contains(
            factory.Name,
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Create<TId>(Guid value)",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Action: Add public static TId",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(typeof(ConcreteEntity<>))]
    [InlineData(typeof(NonGenericEntity))]
    [InlineData(typeof(TwoParameterEntity<,>))]
    [InlineData(typeof(StaticEntity<>))]
    [InlineData(typeof(HiddenEntity<>))]
    [InlineData(typeof(IInvalidEntity<>))]
    public void EntityBaseRejectsWrongShape(Type entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var violation = Assert.Single(
            EntityContractInspection.BaseClass(entity)
        );
        Assert.Contains(
            "public abstract class Entity<TId>",
            violation,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData(typeof(ConcreteEntity<>))]
    [InlineData(typeof(IdEquatableEntity<>))]
    [InlineData(typeof(ObjectEquatableEntity<>))]
    [InlineData(typeof(WrongIdEquatableEntity<>))]
    [InlineData(typeof(NonGenericEntity))]
    public void EntityEqualityRejectsMissingOrIncorrectSelfType(Type entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            DiagnosticAssertions.AssertNoViolations(
                "Entity equality contract",
                EntityContractInspection.Equatable(entity),
                "Implement IEquatable<Entity<TId>> using the entity's own TId."
            )
        );
        Assert.Contains(entity.Name, failure.Message, StringComparison.Ordinal);
        Assert.Contains(
            "IEquatable<Entity<TId>>",
            failure.Message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "Action: Implement",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void MissingEntityIdDiscoveryGivesActionableFailure()
    {
        var failure = Assert.Throws<Xunit.Sdk.TrueException>(() =>
            AggregateContractInspection.SharedType(
                typeof(EntityContractRegressionTests).Assembly,
                "EntityId"
            )
        );
        Assert.Contains("EntityId", failure.Message, StringComparison.Ordinal);
        Assert.Contains("found 0", failure.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Action: Declare EntityId",
            failure.Message,
            StringComparison.Ordinal
        );
    }

    // Public nested fixtures exercise Type.IsVisible. Equality fixtures deliberately
    // check only the interface shape, without providing equality implementations.
#pragma warning disable CA1034, CA1515, CA1067
    public static class ValidFactory
    {
        public static TId Create<TId>(Guid value) =>
            throw new NotSupportedException();
    }

    public sealed class RegularFactory
    {
        public static TId Create<TId>(Guid value)
            where TId : struct => throw new NotSupportedException();
    }

    public interface IInvalidFactory;

    public class GenericFactory<T>;

    private static class HiddenFactory;

    public class MissingFactory;

    public abstract class InstanceFactory
    {
        public abstract TId Create<TId>(Guid value);
    }

    public abstract class ProtectedFactory
    {
        protected static TId Create<TId>(Guid value) =>
            throw new NotSupportedException();
    }

    public static class WrongInputFactory
    {
        public static TId Create<TId>(string value) =>
            throw new NotSupportedException();
    }

    public static class WrongReturnFactory
    {
        public static Guid Create<TId>(Guid value) => value;
    }

    public static class ParameterlessFactory
    {
        public static TId Create<TId>() => throw new NotSupportedException();
    }

    public static class NonGenericFactory
    {
        public static Guid Create(Guid value) => value;
    }

    public static class TwoParametersFactory
    {
        public static TId Create<TId, TOther>(Guid value) =>
            throw new NotSupportedException();
    }

    public abstract class ValidEntity<TId> : IEquatable<ValidEntity<TId>>
    {
        public abstract bool Equals(ValidEntity<TId>? other);
    }

    public class ConcreteEntity<TId>;

    public abstract class NonGenericEntity;

    public abstract class TwoParameterEntity<TId, TOther>;

    public static class StaticEntity<TId>;

    private abstract class HiddenEntity<TId>;

    public interface IInvalidEntity<TId>;

    public abstract class IdEquatableEntity<TId> : IEquatable<TId>
    {
        public abstract bool Equals(TId? other);
    }

    public abstract class ObjectEquatableEntity<TId> : IEquatable<object>
    {
        bool IEquatable<object>.Equals(object? other) =>
            ReferenceEquals(this, other);
    }

    public abstract class WrongIdEquatableEntity<TId>
        : IEquatable<WrongIdEquatableEntity<Guid>>
    {
        public abstract bool Equals(WrongIdEquatableEntity<Guid>? other);
    }
#pragma warning restore CA1034, CA1515, CA1067
}
