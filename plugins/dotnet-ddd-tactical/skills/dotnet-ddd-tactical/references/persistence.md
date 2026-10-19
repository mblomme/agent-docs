# Persistence and concurrency

## Repository boundary

Provide aggregate-oriented loading and storage operations when repositories are needed. Do not create a repository for every child entity or expose arbitrary `IQueryable` across the application boundary. A query can project DTOs without loading aggregates while respecting authorization and bounded-context data ownership.

Place repository interfaces in the domain when fundamental to the aggregate lifecycle, otherwise follow the application's port convention. Implement them in infrastructure. Application workflows own commit boundaries; repository methods that each independently commit can break multi-step atomicity. Pass cancellation tokens through I/O operations.

EF Core's `DbContext` already supplies tracking and unit-of-work behavior. An additional abstraction should express an actual architectural boundary rather than duplicate every EF API. Do not return tracked aggregates to transport callers.

## EF Core mapping

Inspect the installed EF Core version and database provider first. Configure mapping in infrastructure using fluent configuration, keeping ORM annotations and dependencies out of the domain.

- Map encapsulated fields and private setters deliberately. Verify constructor binding: navigation properties are not bound like scalar constructor parameters.
- Choose owned types, complex types, or converters based on supported version, query needs, identity, nullability, and storage shape. They are not interchangeable.
- For converted typed IDs or value objects, verify round trips and query translation. Add a value comparer when the representation requires custom tracking/snapshot semantics.
- Exclude pending domain events from mapping. Materialization must not record new business events.
- Load all state needed by the invariant before executing a command. Lazy loading or partial graphs can make an apparently correct domain method operate on incomplete facts.
- Keep database constraints for unique keys and other race-sensitive rules. An application `AnyAsync` check alone cannot prevent concurrent duplicates.

ORM materialization can bypass constructors or domain creation paths. Verify persisted state restoration with integration tests instead of assuming factory validation runs on load. Do not open public mutation solely to satisfy an ORM.

## Lost updates and retries

Choose optimistic concurrency tokens or an appropriate locking strategy for contested state. Catch persistence concurrency failures in the application/infrastructure boundary and return a meaningful conflict or retry the whole use case when safe. Retrying only `SaveChanges` against stale state does not recheck the invariant.

A retry should reload fresh aggregates, recompute the decision, and discard events from the failed attempt. Bound retries. External effects require idempotency or explicit coordination before retrying.

## Integration checks

Use the production relational provider when validating provider-specific behavior. Verify aggregate round trips, private child collection persistence, value/ID conversion, no events after load, concurrent updates, uniqueness constraints, and rollback of aggregate plus outbox records when those features are in scope. EF's InMemory provider cannot prove transaction or relational semantics; SQLite also differs from other providers.
