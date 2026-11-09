using System.Reflection;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using Type = System.Type;

namespace Company.BoundedContext.DesignTests.Common;

internal static class DomainModelInspection
{
    private const BindingFlags PublicInstance =
        BindingFlags.Public | BindingFlags.Instance;
    private const BindingFlags AllInstance =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    public static IEnumerable<string> Encapsulation(IEnumerable<Type> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        foreach (var type in entities)
        {
            foreach (var field in type.GetFields(PublicInstance))
                if (!field.IsInitOnly && !field.IsLiteral)
                    yield return $"{type.Name}.{field.Name}: public mutable field";
            foreach (var property in type.GetProperties(PublicInstance))
                if (property.SetMethod is { IsPublic: true })
                    yield return $"{type.Name}.{property.Name}: public setter (including init)";
        }
    }

    public static void AggregateBoundaries(
        Assembly[] assemblies,
        AggregateDefinition[] aggregates
    )
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(aggregates);
        var architecture = new ArchLoader().LoadAssemblies(assemblies).Build();
        foreach (var aggregate in aggregates)
        foreach (var other in aggregates.Where(a => a != aggregate))
        foreach (var source in aggregate.Members.Append(aggregate.Root))
        foreach (var child in other.Members)
            DiagnosticAssertions.AssertDependency(
                () =>
                    Types()
                        .That()
                        .Are(source)
                        .Should()
                        .NotDependOnAny(child)
                        .Check(architecture),
                "Aggregate boundaries",
                $"{source.Name} (aggregate {aggregate.Root.Name}) depends on child {child.Name} owned by aggregate {other.Root.Name}.",
                "Remove direct foreign-child access. Invoke behavior through its owning aggregate root in application orchestration; pass identities or immutable snapshots across aggregate boundaries."
            );
    }

    public static void IdentityReferences(
        Assembly[] assemblies,
        AggregateDefinition[] aggregates
    )
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(aggregates);
        var architecture = new ArchLoader().LoadAssemblies(assemblies).Build();
        foreach (var aggregate in aggregates)
        foreach (var other in aggregates.Where(a => a != aggregate))
        foreach (var source in aggregate.Members.Append(aggregate.Root))
            DiagnosticAssertions.AssertDependency(
                () =>
                    Types()
                        .That()
                        .Are(source)
                        .Should()
                        .NotDependOnAny(other.Root)
                        .Check(architecture),
                "Cross-aggregate references",
                $"{source.Name} (aggregate {aggregate.Root.Name}) depends on foreign aggregate root {other.Root.Name}.",
                "Replace foreign aggregate object navigation, parameters, and collections with identity types. Coordinate operations involving multiple roots in the application layer."
            );
    }

    public static IEnumerable<string> ValueObjects(IEnumerable<Type> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        foreach (var type in values)
        {
            if (
                type.GetMethod(nameof(Equals), [typeof(object)])?.DeclaringType
                    is not { } equalityOwner
                || equalityOwner == typeof(object)
                || equalityOwner == typeof(ValueType)
            )
                yield return $"{type.Name}: must override object.Equals";
            if (
                type.GetMethod(
                    nameof(GetHashCode),
                    Type.EmptyTypes
                )?.DeclaringType
                    is not { } hashOwner
                || hashOwner == typeof(object)
                || hashOwner == typeof(ValueType)
            )
                yield return $"{type.Name}: must override GetHashCode";
            foreach (var failure in ImmutableState(type, new HashSet<Type>()))
                yield return failure;
        }
    }

    public static IEnumerable<string> Equality(EqualityExample example)
    {
        ArgumentNullException.ThrowIfNull(example);
        var first = example.First();
        var valueType = first.GetType();
        var equal = example.Equal();
        var different = example.Different();
        if (!first.GetType().IsValueType && ReferenceEquals(first, equal))
            yield return $"{first.GetType().Name}: equality examples must be distinct instances";
        if (!first.Equals(equal))
            yield return $"{valueType.Name}: First.Equals(Equal) returned false for equivalent values. Compare all value components rather than object identity.";
        if (!equal.Equals(first))
            yield return $"{valueType.Name}: Equal.Equals(First) returned false. Make equality symmetric.";
        if (first.GetHashCode() != equal.GetHashCode())
            yield return $"{valueType.Name}: equivalent values have different hash codes. Hash the same components used by Equals.";
        if (first.Equals(different) || different.Equals(first))
            yield return $"{valueType.Name}: distinct values compare equal. Include all meaningful value components in equality and check the Different factory.";
        if (first.Equals(null))
            yield return $"{valueType.Name}: Equals(null) returned true. Return false when the other value is null.";
    }

    private static IEnumerable<string> ImmutableState(
        Type type,
        HashSet<Type> visited
    )
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (
            type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(Guid)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            // A supplied TimeZoneInfo is immutable. Its adjustment-rule accessors
            // return defensive copies; retaining it does not resolve a zone or do I/O.
            || type == typeof(TimeZoneInfo)
        )
            yield break;
        if (!visited.Add(type))
            yield break;
        // Unknown framework containers, interfaces and arrays require an explicit
        // policy rather than assuming that a read-only interface means immutable.
        if (
            type.IsArray
            || type.IsInterface
            || type == typeof(object)
            || type.Namespace?.StartsWith("System", StringComparison.Ordinal)
                == true
                && Nullable.GetUnderlyingType(type) is null
        )
        {
            yield return $"{type.Name}: nested state is not a recognized immutable type";
            yield break;
        }
        foreach (var property in type.GetProperties(AllInstance))
            if (
                property.SetMethod is { } setter
                && !(
                    type.IsValueType
                    && setter
                        .ReturnParameter.GetRequiredCustomModifiers()
                        .Contains(
                            typeof(System.Runtime.CompilerServices.IsExternalInit)
                        )
                )
            )
                yield return $"{type.Name}.{property.Name}: value object setter";
        for (
            var current = type;
            current is not null && current != typeof(object);
            current = current.BaseType
        )
            foreach (
                var field in current.GetFields(
                    AllInstance | BindingFlags.DeclaredOnly
                )
            )
            {
                if (!field.IsInitOnly)
                    yield return $"{current.Name}.{field.Name}: mutable value object state";
                foreach (
                    var failure in ImmutableState(field.FieldType, visited)
                )
                    yield return $"{current.Name}.{field.Name} -> {failure}";
            }
    }
}
