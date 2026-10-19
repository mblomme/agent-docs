# Domain events and reliable workflows

## Record facts inside the domain

Use immutable past-tense event types carrying the facts required by handlers. Snapshot values rather than holding a mutable aggregate reference. Record events in memory after successful state transitions. Domain objects do not dispatch handlers or publish to brokers. A plain domain event interface is sufficient; do not introduce a mediator dependency into the domain merely for dispatch.

Internal domain events belong to one bounded context. Application code explicitly maps selected facts to producer-owned, versioned integration DTOs. Do not serialize entities or reuse internal domain events as public contracts. Supply timestamps and IDs from the application's clock/ID sources when needed.

## Choose dispatch semantics explicitly

For pre-commit local handlers, every durable effect must join the same explicit transaction, with no external side effects. Account for handlers recording more events and prevent unbounded dispatch loops.

Post-commit in-memory handlers are best effort: the process can fail after commit and before dispatch. Use durable work records/outbox messages when losing a follow-up is unacceptable. A handler failure after commit cannot roll back the committed aggregate; avoid reporting it as a safely retryable failed command.

For required external delivery:

```text
begin database transaction
  load current aggregate
  invoke business method
  persist state
  map recorded events to integration messages
  insert outbox rows using the same transaction
commit
clear/acknowledge recorded events
relay committed outbox messages independently
```

Do not clear pending events before their required durable work is safely recorded. After rollback, discard the failed instance and its pending events before retrying. Ensure duplicate snapshots or repeated saves cannot append the same event twice; choose stable message identity and appropriate uniqueness constraints.

The relay publishes, waits for acknowledgement, then marks delivery. A crash between publication and marking can cause redelivery. Design for at-least-once delivery: consumers deduplicate using stable message IDs, and commit the deduplication record atomically with their local state changes. Define ordering per aggregate only when needed; an outbox alone does not guarantee global ordering or exactly-once effects.

External APIs cannot participate in an ordinary database rollback. Coordinate payments or similar effects with idempotency keys, durable workflow state, and compensation appropriate to the business. Do not hold a transaction open and assume a network call becomes atomic.

## Failure checks

When delivery is in scope, verify no publication before commit; aggregate and outbox roll back together; reconstitution emits no events; retrying a conflict uses fresh state; duplicate delivery does not duplicate the business effect; and failure after publication permits safe redelivery. State explicitly when these guarantees remain untested.
