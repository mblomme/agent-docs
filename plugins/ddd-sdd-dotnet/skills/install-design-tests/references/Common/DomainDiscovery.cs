namespace Company.BoundedContext.DesignTests.Common;

internal static class DomainDiscovery
{
    public static Type[] Entities(IEnumerable<Type> types, Type entityBase) =>
        types
            .Where(t => t.IsClass && !t.IsAbstract && Inherits(t, entityBase))
            .ToArray();

    public static Type[] AggregateRoots(
        IEnumerable<Type> types,
        Type aggregateBase
    )
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(aggregateBase);
        return types
            .Where(type =>
                type.IsClass
                && !type.IsAbstract
                && Inherits(type, aggregateBase)
            )
            .ToArray();
    }

    public static bool Inherits(Type type, Type genericBase)
    {
        for (
            var current = type;
            current is not null;
            current = current.BaseType
        )
            if (
                current.IsGenericType
                && current.GetGenericTypeDefinition() == genericBase
            )
                return true;
        return false;
    }

    public static AggregateDefinition[] Aggregates(
        IEnumerable<Type> entities,
        Type aggregateBase
    )
    {
        var all = entities.ToArray();
        var roots = AggregateRoots(all, aggregateBase);
        var failures = roots
            .Where(r =>
                roots.Count(other => other.Namespace == r.Namespace) > 1
            )
            .Select(r =>
                $"{r.FullName}: ambiguous aggregate ownership; multiple roots share this namespace."
            )
            .ToList();
        var members = roots.ToDictionary(r => r, _ => new List<Type>());
        foreach (var child in all.Except(roots))
        {
            var owners = roots
                .Where(r =>
                    child.Namespace == r.Namespace
                    || child.Namespace?.StartsWith(
                        r.Namespace + ".",
                        StringComparison.Ordinal
                    ) == true
                )
                .ToArray();
            if (owners.Length != 1)
                failures.Add(
                    $"{child.FullName}: expected exactly one owning aggregate namespace; found {owners.Length}."
                );
            else
                members[owners[0]].Add(child);
        }
        DiagnosticAssertions.AssertNoViolations(
            "Aggregate ownership discovery",
            failures,
            "Place each root in its own aggregate namespace and folder, with its child entities in that namespace or its descendants. Separate overlapping aggregate namespaces."
        );
        return roots
            .Select(r => new AggregateDefinition(r, members[r].ToArray()))
            .ToArray();
    }
}
