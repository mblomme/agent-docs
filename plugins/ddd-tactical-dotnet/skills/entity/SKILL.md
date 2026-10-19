---
name: entity
description: Build or refactor DDD entities in C# and .NET with stable identity, controlled lifecycle and mutation, and aggregate-owned child behavior.
---

# Entities in .NET

Inspect existing identity types, equality conventions, aggregate ownership, mappings, target framework, and tests. Establish what makes this concept the same entity over time. Preserve the project's libraries, errors, and layout; the pattern alone does not justify new dependencies or base classes.

## Implementation and refactoring

- Use stable identity independent of mutable attributes. Follow existing typed-ID conventions; define the policy for transient/unassigned IDs before relying on equality.
- Prefer a class for entities. Record-generated value equality and copying usually conflict with entity identity and controlled mutation.
- Choose equality deliberately: retain reference equality when appropriate, or implement identity equality consistently with hashing, entity type, and ORM proxy behavior. Never hash mutable business attributes.
- Validate creation and expose business methods instead of public setters. An aggregate-owned child must be created and mutated through its root; a public child method can undermine the boundary even when its collection is read-only.
- Keep restoration separate from new creation. Reconstitution validates persisted structural invariants without replaying business commands, checking today's external eligibility rules, or emitting creation events.
- When removing setters, update callers and hydration together. Keep infrastructure details outside the domain; do not introduce a new architecture or equality hierarchy as an incidental refactor.

## C# example

This child uses reference equality and exposes identity explicitly. The root must keep the child private and return immutable snapshots to external callers. `internal` limits callers but alone does not enforce root ownership within the assembly.

```csharp
internal sealed class OrderLine
{
    public Guid Id { get; }
    public int Quantity { get; private set; }

    private OrderLine(Guid id, int quantity)
    {
        if (id == Guid.Empty) throw new ArgumentException("Identity is required.");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Id = id;
        Quantity = quantity;
    }

    internal static OrderLine Create(Guid id, int quantity)
        => new OrderLine(id, quantity);

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Quantity = quantity;
    }
}
```

## Mistakes and verification

Avoid identity inferred from a name, record copying that duplicates identity, publicly mutable child objects, and event-producing hydration. Do not conflate an ORM entity with a domain entity solely because both have a database key.

Verify identity remains stable across allowed changes, invalid changes leave state intact, and construction rejects invalid input. Test the chosen equality and hash policy explicitly if customized. For persisted entities, verify reconstitution preserves identity and creates no new events; for children, test mutations through the root's public API.
