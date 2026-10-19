---
name: dotnet-ddd-tactical
description: Design, implement, refactor, or review tactical Domain-Driven Design in C# and .NET, including aggregates, entities, value objects, factories, repositories, domain services, specifications, and domain events. Use for invariant modeling, aggregate boundaries, encapsulation, EF Core persistence of domain models, and reliable event workflows. Does not prescribe a service topology or require CQRS or event sourcing.
---

# Tactical DDD in .NET

Start with the business rule and its consistency boundary. A pattern is useful when it protects that rule or expresses the domain language; do not introduce every pattern into a simple CRUD operation.

## Establish context

Inspect repository instructions, architecture documents, project references, target frameworks, nullable settings, persistence configuration, and nearby tests before editing. Keep existing topology, naming, error handling, libraries, and SDK versions unless the task requires a change. Do not add MediatR, a base entity framework, a generic repository, or a new project layout merely to implement DDD.

Identify the bounded context, relevant use case, business terms, invariants, and allowed state transitions. Separate confirmed rules from assumptions. Ask when an unknown rule changes observable behavior; otherwise state a narrow assumption and proceed.

## Select the relevant guidance

- For aggregate boundaries, entity identity, value equality, factories, domain services, and specifications, read [domain-modeling.md](references/domain-modeling.md).
- For repositories, reconstitution, EF Core mapping, and concurrency, read [persistence.md](references/persistence.md).
- For domain events, transaction timing, outbox delivery, and retries, read [events.md](references/events.md).

Read only the references needed by the task. They are design defaults; explicit repository conventions and user requirements take precedence.

## Implement or refactor

1. Express each relevant invariant as a concrete accepted/rejected example. Decide what must change atomically and what may converge later.
2. Put invariant enforcement in the smallest owning domain type. Use named root methods for externally initiated mutations, immutable validated value objects, and typed IDs where they prevent mixing concepts.
3. Keep the domain independent of ORM, HTTP, serialization, DI, and messaging frameworks. Supply time, generated IDs, and external facts as explicit values. Application code coordinates I/O and transactions.
4. Keep creation distinct from reconstitution. Loading existing state must not replay business actions or record creation events.
5. Add persistence or messaging only when required. Check actual framework/provider support before selecting version-sensitive mapping APIs.
6. Verify the behavior with the repository's test tooling. Exercise valid transitions, rejection without partial mutation, value equality, and event payloads where relevant. Use a real relational provider for meaningful persistence/transaction checks; an in-memory fake does not establish relational correctness.

Do not treat a read-only collection interface as proof of encapsulation: callers may recover a mutable backing list, and exposed child objects may still have public mutators. Validate all inputs before changing state, or calculate the next state first.

## Review

Trace a command from its application entrypoint through the aggregate and persistence to commit and event delivery. Identify concrete paths that bypass invariants, change equality, leak mutable state, replay events on load, lose updates, or publish rolled-back facts. Explain the trigger, impact, and smallest useful correction with file locations. Distinguish defects from optional modeling preferences.

## Deliver

For code tasks, implement the scoped change and report the protected rules, tests run, and remaining limits. For design tasks, show the chosen boundary and tradeoffs using a concrete use case. Do not claim tests, schema checks, or client installation succeeded without executing them.
