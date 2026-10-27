---
name: tactical-aggregate
description: Build or refactor DDD aggregates in an object-oriented language for a target project.
---

# Aggregates

The scope, boundary and invariants of an aggregate should already be defined and documented in the product specification. This skill will help you implement the aggregate in C# and .NET. Warn the user if the documentation is missing, vague or contradictory.

## DDD Definition

An aggregate is a cluster of domain objects that can be treated as a single unit. An aggregate has a root and a boundary. The aggregate root is the only member of the aggregate that outside objects are allowed to hold references to, and it enforces all invariants for the aggregate.

## Rules

- Aggregates and AggregateIds should make use of the common base classes.
- Keep external references to other aggregates as typed IDs unless the project has a clear exception.
- Encapsulate child collections behind private fields and read-only views.
- Put domain transitions on public methods of the root and keep child mutation internal to the boundary.
- Create domain events for all documented domain events for that aggregate.
- DomainEvents are raised by a common base class method.
- Check invariants in public methods before changing state by calling intent-revealing `Ensure...` methods.
- When using entities, make sure they make sure they are built according to the entity skill.
- Mutate child entities or collections directly only through the aggregate root.
- Avoid cross-aggregate invariants that require multiple roots to be loaded for one rule.
- Use value objects for descriptive inputs and typed IDs for references.
- Only aggregate roots can raise domain events.
