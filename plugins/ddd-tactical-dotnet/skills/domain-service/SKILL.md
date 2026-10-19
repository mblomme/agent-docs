---
name: domain-service
description: Build or refactor pure DDD domain services in C# and .NET for business calculations or policies that have no natural entity or value-object owner.
---

# Domain services in .NET

Inspect related entities, value objects, application workflows, target framework, and tests. Identify the business decision and explain why no existing domain object naturally owns it. Preserve project conventions and dependencies; a service does not require an interface, DI registration, or a new project.

## Implementation and refactoring

- Keep behavior on its natural entity or value object when it owns the invariant. Use a domain service for a named business calculation or policy spanning supplied concepts without a natural owner.
- Accept domain values, objects, and explicit facts. Return a domain result or decision; let roots enforce state transitions. Do not create a second path around aggregate invariants.
- Keep the service deterministic and free of I/O. Application code loads aggregates and external facts, supplies time or rates, coordinates transactions, and persists results.
- Distinguish domain decisions from application orchestration: HTTP, repositories, message publication, retries, and transaction coordination do not belong in this service.
- When extracting behavior from an application handler, move only the business calculation. Preserve the ordering and ownership of I/O and mutations. Do not move all entity logic into a service layer.

## C# example

Assume the use case prices shipping from destination and weight, with no existing domain object owning this policy. The thresholds are illustrative business rules.

```csharp
public sealed class ShippingPricePolicy
{
    public decimal Calculate(decimal weightKg, bool isDomestic)
    {
        if (weightKg <= 0)
            throw new ArgumentOutOfRangeException(nameof(weightKg));

        var basePrice = isDomestic ? 5m : 15m;
        var excessKg = Math.Max(0m, weightKg - 1m);
        return checked(basePrice + excessKg * 2m);
    }
}
```

Use existing weight/money types where the project models those concepts. If pricing requires a carrier API, the application fetches the quote and supplies it as a fact; the domain service does not call the carrier.

## Mistakes and verification

Avoid anemic entities whose rules all migrate into services, services that resolve dependencies to load aggregates, and calculations that read the clock or configuration implicitly. A domain service must not quietly mutate several aggregates and assume atomic persistence.

Test the business decision table, threshold boundaries, invalid inputs, and determinism for identical supplied facts. Verify inputs remain unchanged. Keep orchestration tests in the application layer; test domain decisions without network, database, or DI setup.
