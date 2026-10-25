---
name: value-object
description: Build or refactor immutable DDD value objects in C# and .NET with validated construction, structural equality, and safe record or typed-ID representations.
---

# Value objects in .NET

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
