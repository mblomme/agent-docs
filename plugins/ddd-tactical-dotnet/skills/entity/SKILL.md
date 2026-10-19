---
name: entity
description: Build or refactor DDD entities in C# and .NET.
---

# Entities in .NET

This skill will help you implement the entity in C# and .NET.

## Workflow

1. Use a typed ID that remains stable throughout the entity's lifecycle.
2. Encapsulate state behind private setters or fields and expose read-only views of child collections.
3. Implement documented domain transitions as methods. Keep creation and mutation of child entities internal to the owning aggregate boundary.
4. Check invariants before changing state by using Ensure methods in the owning aggregate's static assert class.

## Rules

- Declare the entity and its typed ID in the same file.
- Use classes for entities; record-generated value equality and copying do not represent entity identity.
- Only aggregate roots should have repositories. Create and mutate child entities through their aggregate root.
- Use value objects for descriptive inputs and typed IDs for references to other aggregates.
- When the code base contains multiple entities, use a base class or interface to share common behavior and enforce consistency.
- Only the public methods of the aggregate root can raise domain events.
- Common ensure methods to multiple aggregates should be placed in a shared static DomainAsserts class.
- Create a private constructor without parameters for concrete entities to support ORM frameworks. Give abstract base entities a protected constructor without parameters.

## Examples

Example of a common entity implementation, using the shared IEntityId contract:

```csharp
#pragma warning disable S4035
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IEntityId
{
    public TId Id { get; private set; }

    protected Entity(TId id)
    {
        Id = id;
    }

    protected Entity() { }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return Equals((Entity<TId>)obj);
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (other.GetType() != GetType())
            return false;

        // Optimized: Compare the primitive GUID values directly
        return Id.Value.Equals(other.Id.Value);
    }

    public override int GetHashCode()
    {
        return Id.Value.GetHashCode();
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !(left == right);
    }

    public virtual void ValidateState()
    {
        // This method can be overridden by derived classes to implement state validation logic
    }
}
#pragma warning restore S4035
```

Example of a typed entity ID:

```csharp
public readonly record struct OrderLineId(Guid Value) : IEntityId;
```
