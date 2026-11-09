// Fixtures are inspected as metadata by reflection and ArchUnitNET.
#pragma warning disable CA1812

namespace Company.BoundedContext.DesignTests.Common;

internal static class RegressionFixtures
{
    internal sealed class MutableEntity
    {
        public int Number = 1;
        public string Name { get; set; } = "";
    }

    internal sealed class EncapsulatedEntity
    {
        public int Number { get; private set; }
    }

    internal sealed record NestedMutableValue(List<string> Items);

    internal sealed class ReferenceValue;

    internal sealed record BrokenEquality(int Number)
    {
        public bool Equals(BrokenEquality? other) => other is not null;

        public override int GetHashCode() => 0;
    }

    internal interface IRepository;

    internal interface IRepository<T>;

    internal interface SomeAggregateName : IRepository;

    internal interface AnotherAggregateName : IRepository<OtherRoot>;

    internal interface IndirectAggregateName : SomeAggregateName;

    internal interface IRepositoryPort<T>;

    internal sealed class Repository : IRepositoryPort<OtherRoot>;

    internal sealed class OtherRoot;

    internal sealed class Child
    {
        public int Version { get; private set; }

        public void Mutate() => Version++;
    }

    internal sealed class InvadingRoot
    {
        public static void Invade(Child child) => child.Mutate();
    }

    internal sealed class NavigatingRoot
    {
        public List<OtherRoot> Others { get; } = [];
    }

    internal sealed class IdentityRoot
    {
        public Guid OtherId { get; private set; }
    }

    internal sealed class EventfulAggregate
    {
        public int Version { get; private set; }

        public EventfulAggregate() => RecordEvent();

        public void Change() => ChangeThroughHelper();

        private void ChangeThroughHelper() => RecordEvent();

        private void RecordEvent() => Version++;

        public int Query() => Version;

        public static EventfulAggregate Reconstitute() => new();
    }

    internal sealed class SilentAggregate
    {
        public int Version { get; private set; }

        public SilentAggregate() { }

        public void Change() => Version++;

        public void RecursiveChange()
        {
            Version++;
            if (Version < 2)
                RecursiveChange();
        }

        public int Query() => Version;

        public static SilentAggregate Reconstitute() => new();
    }
}
