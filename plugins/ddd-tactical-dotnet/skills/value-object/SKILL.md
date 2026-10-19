---
name: value-object
description: Build or refactor immutable DDD value objects in C# and .NET with validated construction, structural equality, and safe record or typed-ID representations.
---

# Value objects in .NET

## Workflow

1. Validate and normalize at creation so invalid instances cannot exist.
2. Keep the value object immutable and equality-by-value.
3. Replace repeated primitive validation in entities or aggregates with the new value object.

## Rules

- Prefer `sealed record` or `readonly record struct` when local conventions allow.
- Use named factories when construction needs validation or normalization.
- Keep value objects free of persistence, serialization, request binding, and display formatting concerns.
- Do not expose mutating methods or public setters.
- Use domain errors or exceptions consistent with the project.
- Value objects must extend a common base class like in the example below.

## Example

An example of a value object base class.

```csharp
#pragma warning disable S2094
public abstract record ValueObject; // This is an intentional marker type for DDD Value Objects
#pragma warning restore S2094
```

