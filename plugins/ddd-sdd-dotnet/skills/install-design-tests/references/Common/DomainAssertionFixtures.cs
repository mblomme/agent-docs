// Fixtures require public visibility for reflection shape checks.
#pragma warning disable CA1034, CA1515
namespace Company.BoundedContext.DesignTests.Common.AssertionFixtures.Shared
{
    using Company.BoundedContext.DesignTests.Common.AssertionFixtures.Aggregates;

    public interface IFixtureId
    {
        Guid Value { get; }
    }

    public readonly record struct FixtureId(Guid Value) : IFixtureId;

    public sealed class FixtureDomainException : Exception
    {
        public FixtureDomainException() { }

        public FixtureDomainException(string message)
            : base(message) { }

        public FixtureDomainException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    public static class ValidGuards
    {
        public static void EnsureNotNull(object? value) =>
            ArgumentNullException.ThrowIfNull(value);

        public static T EnsureNotNull<T>(T? value, string parameterName)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(value, parameterName);
            return value;
        }

        public static void EnsureNotNullOrWhiteSpace(string? value) =>
            EnsureNotNullOrWhiteSpace(value, "value");

        public static string EnsureNotNullOrWhiteSpace(
            string? value,
            string parameterName
        )
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "Text must contain a meaningful value.",
                    parameterName
                );
            return value;
        }

        public static void EnsureValidId(IFixtureId id, string message)
        {
            ArgumentNullException.ThrowIfNull(id);
            if (id.Value == Guid.Empty)
                throw new FixtureDomainException(message);
        }

        // Catalogue tests must not execute or require the optional boolean primitive.
        public static void Ensure(bool condition, string message) =>
            throw new InvalidOperationException(
                "The optional primitive must not be invoked."
            );

        public static void EnsureSharedValues(
            Dictionary<string, List<FixtureId[]>> values
        ) => ArgumentNullException.ThrowIfNull(values);
    }

    public static class NamedGuardsOnly
    {
        public static void EnsureNotNull(object? value) =>
            ValidGuards.EnsureNotNull(value);

        public static void EnsureNotNullOrWhiteSpace(string? value) =>
            ValidGuards.EnsureNotNullOrWhiteSpace(value);

        public static void EnsureValidId(IFixtureId id) =>
            ValidGuards.EnsureValidId(id, "Identity must not be empty.");
    }

    public static class NoGuards;

    public static class NoOpNullGuard
    {
        public static void EnsureNotNull(object? value) { }
    }

    public static class InvertedNullGuard
    {
        public static void EnsureNotNull(object? value)
        {
            if (value is not null)
                throw new ArgumentException("Unexpected value.", nameof(value));
        }
    }

    public static class WrongExceptionTextGuard
    {
        public static void EnsureNotNullOrWhiteSpace(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Text is blank.");
        }
    }

    public static class BlankMessageTextGuard
    {
        public static void EnsureNotNullOrWhiteSpace(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new FixtureDomainException("");
        }
    }

    public static class BrokenOverloadTextGuard
    {
        public static void EnsureNotNullOrWhiteSpace(string? value) =>
            ValidGuards.EnsureNotNullOrWhiteSpace(value);

        public static void EnsureNotNullOrWhiteSpace(
            string? value,
            string name
        ) { }
    }

    public static class AsyncTextGuard
    {
        public static Task EnsureNotNullOrWhiteSpace(string? value) =>
            Task.CompletedTask;
    }

    public static class RestrictedGenericNullGuard
    {
        public static void EnsureNotNull<T>(T? value)
            where T : AggregateSpecificValue =>
            ArgumentNullException.ThrowIfNull(value);
    }

    public abstract class InstanceNullGuard
    {
        public abstract void EnsureNotNull(object? value);
    }

    public static class WrongInputNullGuard
    {
        public static void EnsureNotNull(string? value) =>
            ArgumentNullException.ThrowIfNull(value);
    }

    public static class WrongTrailingNullGuard
    {
        public static void EnsureNotNull(object? value, int count) =>
            ArgumentNullException.ThrowIfNull(value);
    }

    public static class WrongNames
    {
        public static void Meaningful(string? value) { }

        public static void Ensurelowercase() { }

        // Deliberately invalid spelling verifies the Ensure/PascalCase naming rule.
#pragma warning disable CA1707
        public static void Ensure_BadName() { }
#pragma warning restore CA1707

        public static void Ensure() { }
    }

    public static class AggregateSpecificGuards
    {
        public static AggregateSpecificValue EnsureReturn() =>
            throw new NotSupportedException();

        public static void EnsureDirect(AggregateSpecificValue value) =>
            ArgumentNullException.ThrowIfNull(value);

        public static void EnsureNested(
            Dictionary<string, List<AggregateSpecificValue[]>> value
        ) => ArgumentNullException.ThrowIfNull(value);

        public static void EnsureConstrained<T>(T value)
            where T : AggregateSpecificValue =>
            ArgumentNullException.ThrowIfNull(value);
    }

    public static class ShapeFixtures
    {
        public class InstanceAssertions;

        public abstract class AbstractAssertions;

        public interface IAssertions;

        public readonly record struct StructAssertions;

        public static class GenericAssertions<T>;

        private static class HiddenAssertions;

        public static Type HiddenType => typeof(HiddenAssertions);
    }
}

namespace Company.BoundedContext.DesignTests.Common.AssertionFixtures.Aggregates
{
    public class AggregateSpecificValue;
}
#pragma warning restore CA1034, CA1515
