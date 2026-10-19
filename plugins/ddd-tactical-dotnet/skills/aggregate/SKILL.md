---
name: aggregate
description: Build or refactor DDD aggregates in C# and .NET, including consistency boundaries, root-controlled state transitions, and protection of invariants across owned entities.
---

# Aggregates in .NET

Inspect the target framework, nearby domain types, persistence mappings, error conventions, and tests. Identify the business rule and what must change atomically before choosing the boundary. Preserve project conventions; do not add a base framework, packages, CQRS, or a new topology merely to introduce this pattern.

## Implementation and refactoring

- Put externally initiated changes behind named root methods. Validate the entire transition before changing state so rejected operations leave no partial mutation.
- Keep owned entity construction and mutation under root control. A read-only collection interface does not prevent casting to its mutable backing collection; expose a wrapper or immutable snapshot and inspect child mutators too.
- Reference other aggregates by identity, preferably typed IDs when supported by existing conventions. Do not grow the boundary merely to navigate object graphs.
- Supply time, generated IDs, and external facts as inputs. Application code coordinates I/O and transactions; the domain stays independent of ORM and transport frameworks.
- Separate validated creation from reconstitution. Loading persisted state must not replay commands or produce creation events. If events are used, collect facts only after successful transitions; application code controls dispatch.
- When refactoring public setters, trace all callers and persistence hydration paths before replacing them. Preserve observable behavior except where the requested invariant requires a change.

## C# example

This small root owns a quantity invariant. Use the project's normal error representation in real code.

```csharp
public sealed class Reservation
{
    public Guid Id { get; }
    public Guid ProductId { get; } // Another aggregate's identity
    public int Quantity { get; private set; }

    private Reservation(Guid id, Guid productId, int quantity)
    {
        if (id == Guid.Empty || productId == Guid.Empty)
            throw new ArgumentException("Identity is required.");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Id = id;
        ProductId = productId;
        Quantity = quantity;
    }

    public static Reservation Create(Guid id, Guid productId, int quantity)
        => new Reservation(id, productId, quantity);

    public void Increase(int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var next = checked(Quantity + amount);
        Quantity = next;
    }
}
```

## Mistakes and verification

Avoid roots that only expose setters, child methods that bypass root invariants, and cross-aggregate graph mutations hidden inside one command. Do not assume every multi-aggregate workflow needs one large aggregate.

Test valid transitions, boundary values, overflow, and rejection without state changes. For roots with children, verify callers cannot mutate the collection or a child to bypass rules. Where persistence or events are involved, verify round trips preserve invariants and loading records no new events.
