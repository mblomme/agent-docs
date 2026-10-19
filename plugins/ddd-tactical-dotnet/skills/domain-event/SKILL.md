---
name: domain-event
description: Build or refactor DDD domain events in C# and .NET, including immutable business facts, aggregate event recording, transaction-aware dispatch, and reliable integration-event delivery.
---

# Domain events in .NET

Inspect aggregate methods, event handlers, persistence, transaction boundaries, target framework, and existing messaging conventions. Identify the business fact and when it becomes true. Preserve existing libraries and architecture; domain events do not require MediatR, event sourcing, CQRS, a broker, or a new base class.

## Implementation and refactoring

- Name events as past-tense business facts using the domain language. Record them only after a successful state transition, with immutable payloads containing the facts consumers need. Avoid mutable entity references, ORM objects, and transport DTOs.
- Let aggregate roots collect events without publishing them or calling handlers. Supply timestamps and generated identities explicitly rather than sampling clocks or randomness in the domain.
- Keep creation separate from reconstitution: loading persisted state must not record fresh business events. Refactor command and hydration paths together when introducing event recording.
- Make dispatch timing explicit in the application layer. In-process handlers before commit may participate in the same transaction, but must not perform irreversible external effects. Handler failure must roll back the intended unit of work.
- Post-commit dispatch cannot roll back committed state. If delivery must survive a process crash, persist pending delivery in the same transaction as the aggregate, then dispatch through a retryable relay. Do not assume an in-memory event list guarantees delivery.
- Distinguish internal domain events from public integration contracts. Map selected facts to versioned integration messages at the application boundary. Use an outbox when reliable external publication is required; do not publish uncommitted facts to a broker.
- Preserve message identity across delivery retries and make consumers idempotent. Specify ordering only where the business requires it; do not promise exactly-once effects from at-least-once delivery.
- Drain or clear recorded events at the lifecycle point appropriate to the existing unit of work. Do not discard the only delivery record before durable capture succeeds. Account for handlers recording additional events without accidental unbounded redispatch.

## C# example

This example requires C# 9 records. It records an immutable fact and exposes a copied event list. The application persists changes and controls delivery; `PendingEvents` alone provides no crash recovery.

```csharp
public sealed record OrderConfirmed(Guid OrderId, DateTimeOffset OccurredAt);

public sealed class Order
{
    private readonly List<OrderConfirmed> events = new List<OrderConfirmed>();
    public Guid Id { get; }
    public bool IsConfirmed { get; private set; }
    public IReadOnlyList<OrderConfirmed> PendingEvents => events.ToArray();

    private Order(Guid id, bool isConfirmed)
    {
        if (id == Guid.Empty) throw new ArgumentException("Identity is required.");
        Id = id;
        IsConfirmed = isConfirmed;
    }

    public static Order Create(Guid id) => new Order(id, false);
    public static Order Reconstitute(Guid id, bool isConfirmed)
        => new Order(id, isConfirmed);

    public void Confirm(DateTimeOffset occurredAt)
    {
        if (IsConfirmed) throw new InvalidOperationException("Already confirmed.");
        IsConfirmed = true;
        events.Add(new OrderConfirmed(Id, occurredAt));
    }
}
```

The example rejects repeated confirmation; follow the actual use case's retry policy. Use the project's existing event abstraction and event-clearing mechanism where present. Add message IDs when reliable delivery requires deduplication, preserving them in durable storage rather than regenerating them on each publish attempt.

## Mistakes and verification

Avoid events that describe commands rather than completed facts, mutable payloads, publication from domain objects, creation events during hydration, and broker calls inside database transactions presented as atomic delivery.

Test that valid transitions record the expected payload, rejected transitions record nothing and leave state unchanged, and reconstitution records no events. Verify callers cannot mutate the recorded facts or backing collection. For transactional handlers, test rollback on failure. When an outbox is used, test aggregate/outbox atomicity with the actual database provider, recovery after commit but before publication, stable IDs on retries, and duplicate-safe consumers. Keep simple event-recording tests independent of messaging infrastructure.
