---
name: repository
description: Build or refactor DDD repositories in C# and .NET for aggregate persistence, including persistence-agnostic contracts, event-free reconstitution, transaction ownership, and optimistic concurrency.
---

# Repositories in .NET

Inspect aggregate boundaries, existing repository contracts, unit-of-work behavior, target framework, database provider, mappings, and integration tests. Preserve the project's architecture, error policy, and dependencies. Do not add a generic repository or replace an ORM merely to apply this pattern.

## Implementation and refactoring

- Model repositories around aggregate roots, not every table or child entity. Put a persistence-agnostic contract in the domain when fundamental to the aggregate lifecycle, or follow the established port placement.
- Expose use-case-relevant load/store operations using domain types and identities. Define not-found and cancellation behavior. Avoid exposing `DbContext`, provider exceptions, or `IQueryable` through the contract.
- Keep query projections separate when aggregate loading is unnecessary. A repository need not become a reporting API or implement every possible CRUD method.
- Implement persistence in infrastructure. Restore the complete consistency boundary through event-free reconstitution; do not invoke creation workflows during loading. Keep ORM configuration outside the domain and follow the project's mapping strategy.
- Make commit ownership explicit. Application/unit-of-work code should coordinate the transaction; repository methods must not independently commit when several writes need one transaction. If an outbox exists, persist its records in that same transaction.
- Use the existing version/concurrency token to detect lost updates. Translate conflicts to the established application error. Retry only with a deliberate reload and business-rule reevaluation; do not blindly replay external effects.
- When refactoring, trace tracking, child deletion, cancellation, and transaction behavior before changing signatures. Verify framework/provider support for any version-sensitive mapping APIs against official documentation.

## C# example

This port uses nullable reference types and an explicit version token. `Stage` queues a write; the application-owned unit of work commits it and checks the expected version atomically. Use an existing repository/unit-of-work convention instead when available.

```csharp
public interface IOrderRepository
{
    Task<LoadedOrder?> FindAsync(Guid id, CancellationToken cancellationToken);
    void Stage(Order order, long expectedVersion);
}

public sealed class LoadedOrder
{
    public Order Aggregate { get; }
    public long Version { get; }

    public LoadedOrder(Order aggregate, long version)
    {
        Aggregate = aggregate ?? throw new ArgumentNullException(nameof(aggregate));
        Version = version;
    }
}
```

`Order` is the application's aggregate root. The infrastructure implementation must enforce the expected version in the database write, not merely compare it in memory. Creation operations and the unit-of-work interface are intentionally left to the target application's needs.

## Mistakes and verification

Avoid per-child repositories, partial aggregate loading that bypasses invariants, hidden commits, event-producing hydration, and fake concurrency checks performed before an unconditional update.

Use the actual relational provider for transaction and concurrency integration tests when those semantics matter; an in-memory fake cannot establish them. Verify round trips including owned children/value objects, missing IDs, event-free loading, rollback, and two writers conflicting on the same version. Exercise cancellation and exception translation according to the contract. If outbox writes are involved, verify aggregate and outbox changes commit or roll back together.
