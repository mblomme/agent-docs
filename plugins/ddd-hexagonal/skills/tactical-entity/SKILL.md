---
name: tactical-entity
description: Build or refactor DDD entities in an object-oriented language for a target project, including unique identity, lifecycle, and mutable state.
---

# Entities

This skill will help you implement the entity in target project.

## DDD Definition

An entity is a domain object that has a unique identity and a lifecycle. Entities are mutable and can change state over time, but their identity remains constant. They are used to model concepts in the domain that have a distinct identity and can be tracked throughout their lifecycle.

## Rules

- Encapsulate state behind private setters or fields and expose read-only views of child collections.
- Implement documented domain transitions as methods. Keep creation and mutation of child entities internal to the owning aggregate boundary.
- Check invariants before changing state by calling intent-revealing `Ensure...` methods.
- Make use of the common base classes for entities and entity IDs.
- Use value objects for descriptive inputs and typed IDs for references to other aggregates.