---
name: code-value-object
description: Build or refactor immutable DDD value objects in an object-oriented language for a target project, including descriptive attributes, equality-by-value, and safe construction.
---

# Value objects

Follow the target project's programming language, toolchain, and conventions.
Express these object-oriented design rules using the language's supported
constructs; do not assume a particular framework, SDK, or test library.

## DDD Definition

A value object is a domain object that represents a descriptive aspect of the domain with no conceptual identity. Value objects are immutable and are defined by their attributes rather than a unique identity. They are used to model concepts in the domain that can be fully described by their properties and can be compared based on their values.

## Workflow

- Value objects must make use of a common base class.
- Validate and normalize at creation so invalid instances cannot exist.
- Keep the value object immutable and equality-by-value.
- Replace repeated primitive validation in entities or aggregates with the new value object.
- Use named static factories when construction needs validation or normalization.
- Keep value objects free of persistence, serialization, request binding, and display formatting concerns.
- Do not expose mutating methods or public setters.
- Use domain errors or exceptions consistent with the project.
