---
name: value-object
description: Build or refactor immutable DDD value objects in C# and .NET with validated construction, structural equality, and safe record or typed-ID representations.
---

# Value objects in .NET

Inspect the target C# version, nullable settings, existing value types, serializers, mappings, and error conventions. Establish the concept's meaning, equality components, and actual business constraints. Preserve existing dependencies and architecture.

## Implementation and refactoring

- Make invalid values unconstructible through supported public APIs. Use validated constructors or factories and keep components immutable, including nested collections and objects.
- Define normalization only when the domain requires it; do not silently trim, case-fold, round, or convert units. Equality and hash codes must use the same semantic components.
- Records help with structural equality but public `init` properties and `with` can bypass constructor validation. Prefer get-only components or enforce invariants through every supported construction path.
- Record equality on arrays and many collections compares references rather than elements. Implement sequence semantics deliberately when needed and copy mutable inputs.
- Structs always allow `default`; a private constructor cannot prevent an invalid zero value. Choose a class or an explicit invalid/default policy. Typed IDs should prevent mixing distinct concepts without assuming every primitive needs a wrapper.
- Check serialization and persistence materialization paths. Configure conversion in infrastructure rather than adding framework attributes to the domain. When replacing primitives, migrate call sites and stored representation deliberately.

## C# example

This example requires C# 9 records. Adapt to a sealed class with explicit equality on older targets. Get-only state prevents `with` from assigning an invalid amount.

```csharp
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (currency != "EUR" && currency != "USD")
            throw new ArgumentException("Unsupported currency.", nameof(currency));
        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        if (Currency != other.Currency)
            throw new InvalidOperationException("Currencies must match.");
        return new Money(checked(Amount + other.Amount), Currency);
    }
}
```

The currencies and nonnegative amount are example domain rules, not universal money requirements. Production rules must come from the use case.

## Mistakes and verification

Avoid mutable record members, equality that ignores units/currency, and normalization without a business rule. Do not assume nullable annotations enforce non-null values at runtime.

Test equal values and equal hashes, unequal semantic components, invalid and null input, boundary values, and operations that leave originals unchanged. Where relevant, verify defensive copies, default struct handling, and persistence/serialization round trips without validation bypass.
