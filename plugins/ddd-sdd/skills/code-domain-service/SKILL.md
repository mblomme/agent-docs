---
name: code-domain-service
description: Create or refactor stateless DDD domain services in an object-oriented language for a target project. Use when a domain rule or calculation spans concepts and does not naturally belong to one aggregate or value object, while avoiding application orchestration, infrastructure adapters, dependency injection, and side effects.
---

# DDD Domain Service

Follow the target project's programming language, toolchain, and conventions.
Express these object-oriented design rules using the language's supported
constructs; do not assume a particular framework, SDK, or test library.

## DDD Definition

A domain service is a stateless service that encapsulates domain logic or calculations that do not naturally belong to a single aggregate or value object. Domain services are used when a domain rule or operation spans multiple concepts and cannot be easily placed on an aggregate, entity, or value object.

## Workflow

1. First try to place the behavior on an aggregate, entity, or value object.
2. Use a domain service only when the rule truly spans multiple domain concepts or is awkward on a single model object.
3. The domain service is stateless and name methods in domain language.
4. Depend only on domain concepts or domain-owned abstractions.
5. Return domain values, booleans, or domain errors; do not coordinate application workflows.
6. Test the rule with deterministic inputs and no external services
7. The name of a domain service class and file must end with the word Service.

## Rules

- Domain services express domain decisions, not use case orchestration.
- Do not inject application ports, logging, configuration, or repositories.
- Prefer pure functions when possible.
- Keep side effects inside aggregate behavior, not hidden in services.
