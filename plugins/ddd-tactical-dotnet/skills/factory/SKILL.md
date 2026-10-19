---
name: factory
description: Build or refactor DDD factories in C# and .NET for creating valid aggregates, including complex construction, owned entities, creation invariants, and separation from persistence reconstitution.
---

# Aggregate factories in .NET

Inspect aggregate constructors, creation methods, owned entities, application workflows, persistence hydration, target framework, and tests. Identify the creation invariants and the inputs needed to build a complete aggregate. Preserve existing error handling, libraries, and architecture; do not add a factory interface, DI registration, or base framework unless the use case needs it.

## Implementation and refactoring

- Use a named static creation method when construction is simple. Introduce a separate factory when assembling a valid aggregate requires meaningful domain decisions or substantial coordination of owned objects. Avoid a factory that merely forwards constructor arguments without improving the model.
- Return a complete, valid aggregate. Keep constructors and child construction controlled so callers cannot create partially initialized roots or bypass invariants through a different public path.
- Keep rules enforced by the aggregate or its value objects even when a factory helps assemble inputs. A factory must not become the only guard for invariants that public aggregate operations can violate later.
- Pass identities, time, and external facts explicitly. Application code loads reference data and calls external services before invoking the factory; domain construction does not query repositories, resolve services, or persist results.
- Let the root own child entity creation and attachment. A factory may calculate a construction plan, but it must not expose child setters, leak mutable collections, or use reflection to bypass the root's boundary.
- Separate new creation from reconstitution. A repository restores persisted state through a dedicated path, without applying today's creation policy, generating replacement IDs, or recording new creation events. Validate structural invariants on restoration.
- If creation records a domain event, record it exactly once after successful construction through the project's existing root mechanism. Publication and transaction handling remain in the application layer.
- When refactoring constructors or builders, trace all creation and hydration callers. Preserve compatibility intentionally and remove public construction paths that bypass the intended rules. A fluent builder must not expose an incomplete aggregate before final validation.

## C# example

This named factory method is sufficient for a simple reservation aggregate. The application supplies identity and time. More complex assembly can use a separate domain factory that calls the same controlled root creation API.

```csharp
public sealed class Reservation
{
    public Guid Id { get; }
    public Guid ProductId { get; }
    public int Quantity { get; }
    public DateTimeOffset ExpiresAt { get; }

    private Reservation(Guid id, Guid productId, int quantity,
        DateTimeOffset expiresAt)
    {
        Id = id;
        ProductId = productId;
        Quantity = quantity;
        ExpiresAt = expiresAt;
    }

    public static Reservation Create(Guid id, Guid productId, int quantity,
        DateTimeOffset now, TimeSpan lifetime)
    {
        if (id == Guid.Empty || productId == Guid.Empty)
            throw new ArgumentException("Identity is required.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime));

        var expiresAt = now.Add(lifetime);
        return new Reservation(id, productId, quantity, expiresAt);
    }
}
```

The expiration rule is illustrative. Do not infer inventory availability from this constructor: if availability matters, model its consistency boundary explicitly rather than treating a previously fetched quantity as a concurrency guarantee. A persistence restoration path would accept the stored expiration directly instead of recalculating it from a new clock value.

## Mistakes and verification

Avoid factories that save the aggregate, hide network calls, sample the clock, return invalid intermediate objects, or duplicate all invariant logic outside the root. Do not use the new-creation path for hydration or let a general-purpose object mapper bypass controlled construction.

Test valid construction, missing identities, invalid and boundary inputs, and derived values from supplied facts. For complex factories, verify owned entities are complete, input collections cannot mutate the resulting aggregate, and failures expose no partial aggregate or side effects. If events are recorded, verify one creation fact on success and none on rejection or reconstitution. Test persistence restoration separately, including preservation of original IDs and timestamps.
