---
name: domain-service
description: Create or refactor stateless C# DDD domain services in a project-agnostic Domain Layer. Use when a domain rule or calculation spans concepts and does not naturally belong to one aggregate or value object, while avoiding application orchestration, infrastructure adapters, dependency injection, and side effects.
---

# DDD Domain Service

## Workflow

1. First try to place the behavior on an aggregate, entity, or value object.
2. Use a domain service only when the rule truly spans multiple domain concepts or is awkward on a single model object.
3. Keep the service stateless and name methods in domain language.
4. Depend only on domain concepts or domain-owned abstractions.
5. Return domain values, booleans, or domain errors; do not coordinate application workflows.
6. Test the rule with deterministic inputs and no external services.

## Rules

- Domain services express domain decisions, not use case orchestration.
- Do not inject application ports, logging, configuration, clocks from infrastructure, or repositories unless the repository interface is domain-owned and the rule genuinely needs it.
- Prefer pure functions when possible.
- Keep side effects inside aggregate behavior, not hidden in services.
