// Reflection-only fixtures intentionally need no construction.
#pragma warning disable CA1812

namespace Company.BoundedContext.DesignTests.Common
{
    public class AggregateAssertionRegressionTests
    {
        [Fact]
        public void EmptyAndPopulatedClassesAreAccepted()
        {
            var roots = new[]
            {
                typeof(AssertionFixtures.Empty),
                typeof(AssertionFixtures.Valid),
            };
            Assert.Empty(
                AggregateAssertionInspection.Classes(
                    typeof(AssertionFixtures.Empty).Assembly,
                    roots
                )
            );
            Assert.Empty(
                AggregateAssertionInspection.Naming(
                    typeof(AssertionFixtures.Empty).Assembly,
                    roots
                )
            );
        }

        [Theory]
        [InlineData(typeof(AssertionFixtures.Missing))]
        [InlineData(typeof(AssertionFixtures.WrongName))]
        [InlineData(typeof(AssertionFixtures.WrongNamespace))]
        [InlineData(typeof(AssertionFixtures.Generic))]
        public void MissingOrInvalidClassesProduceActionableDiagnostics(
            Type root
        )
        {
            ArgumentNullException.ThrowIfNull(root);
            var message = Assert.Single(
                AggregateAssertionInspection.Classes(root.Assembly, [root])
            );
            Assert.Contains(
                root.Name + "Asserts",
                message,
                StringComparison.Ordinal
            );
            Assert.Contains("Declare", message, StringComparison.Ordinal);
        }

        [Fact]
        public void NoDiscoveredRootsIsAnExplicitFailure() =>
            Assert.Single(
                AggregateAssertionInspection.Classes(
                    typeof(AggregateAssertionRegressionTests).Assembly,
                    []
                )
            );

        [Fact]
        public void EveryPublicAndInternalOverloadIsCheckedWhilePrivateHelpersAreAllowed()
        {
            var violations = AggregateAssertionInspection
                .Naming(typeof(AssertionFixtures.InvalidAsserts))
                .ToArray();
            Assert.Equal(5, violations.Length);
            foreach (
                var name in new[]
                {
                    "Ensure()",
                    "Ensurelowercase",
                    "Ensure_Bad",
                    "Check(Int32)",
                    "Check(System.String)",
                }
            )
                Assert.Contains(
                    violations,
                    message => message.Contains(name, StringComparison.Ordinal)
                );
            Assert.DoesNotContain(
                violations,
                message => message.Contains("Helper", StringComparison.Ordinal)
            );
        }
    }
}

namespace Company.BoundedContext.DesignTests.Common.AssertionFixtures
{
    internal sealed class Empty;

    internal static class EmptyAsserts;

    internal sealed class Valid;

    internal sealed class ValidAsserts
    {
        public static void EnsureValid() => Helper();

        internal int EnsureSomethingElse() => Value;

        private static void Helper() { }

        public int Value { get; }
    }

    internal sealed class Missing;

    internal sealed class WrongName;

    internal static class WrongNameAssertions;

    internal sealed class WrongNamespace;

    internal sealed class Generic;

    internal sealed class GenericAsserts<T>;

    internal static class InvalidAsserts
    {
        public static void Ensure() { }

        public static void Ensurelowercase() { }

        internal static void Ensure_Bad() { }

        public static void Check(int value) { }

        internal static void Check(string value) { }

        private static void Helper() { }
    }
}

namespace Company.BoundedContext.DesignTests.Common.OtherAssertionFixtures
{
    internal static class WrongNamespaceAsserts;
}
