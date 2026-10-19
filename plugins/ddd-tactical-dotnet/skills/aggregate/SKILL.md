---
name: aggregate
description: Build or refactor DDD aggregates in C# and .NET.
---

# Aggregates in .NET

The scope, boundary and invariants of an aggregate should already be defined and documented in the domain model. This skill will help you implement the aggregate in C# and .NET. Warn the user if the documentation is missing, vague or contradictory.

## Workflow

1. Keep external references to other aggregates as typed IDs unless the project has a clear exception.
2. Encapsulate child collections behind private fields and read-only views.
3. Put domain transitions on public methods of the root and keep child mutation internal to the boundary.
4. Create domain events for all documented domain events for that aggregate.
5. Invariants must be checked in public methods by using Ensure methods. These ensure methods should be bundled in a seperate assert static class per aggregate.


## Rules

- Declare the Aggregate and its AggregateId in the same file.
- Only aggregate roots should have repositories.
- Mutate child entities or collections directly only through the aggregate root.
- Avoid cross-aggregate invariants that require multiple roots to be loaded for one rule.
- Use value objects for descriptive inputs and typed IDs for references.
- When the code base contains multiple aggregates, use a base class or interface to share common behavior and enforce consistency.
- Only the public methods of the aggregate can raise domain events.
- Common ensure methods to multiple aggregates should be placed in a shared static DomainAsserts class.
- Create a private constructor without parameters for the aggregate root to support ORM frameworks.
- Creation of an aggregate should be done through a static factory method on the aggregate root. This means the constructor of the aggregate root should be private and the factory method should be public and static. The factory method should return a new instance of the aggregate root.


## Examples

Example of a common aggregate ID implementation:

```csharp
public interface IEntityId
{
    Guid Value { get; }
}

public static class EntityId
{
    private static readonly ConcurrentDictionary<Type, Delegate> _constructors =
        new();

    public static TId New<TId>(Guid? guid = default)
        where TId : struct, IEntityId
    {
        var factory =
            (Func<Guid, TId>)
                _constructors.GetOrAdd(typeof(TId), CreateFactory<TId>);

        Guid id = guid ?? Guid.CreateVersion7();

        return factory(id);
    }

    private static Func<Guid, TId> CreateFactory<TId>(Type type)
        where TId : struct
    {
        var constructor =
            type.GetConstructor([typeof(Guid)])
            ?? throw new InvalidOperationException(
                $"Type {type.Name} must have a constructor accepting a Guid."
            );

        var param = Expression.Parameter(typeof(Guid), "value");
        var newExpr = Expression.New(constructor, param);
        var lambda = Expression.Lambda<Func<Guid, TId>>(newExpr, param);

        return lambda.Compile();
    }
}
```

Example of a common aggregate root implementation:

```csharp
public interface IAggregateRoot
{
    long ConcurrencyVersion { get; }
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : struct, IEntityId
{
    public long ConcurrencyVersion { get; private set; }
    private readonly IList<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    protected AggregateRoot(TId id)
        : base(id) { }

    protected AggregateRoot() { }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
```
