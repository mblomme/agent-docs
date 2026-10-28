---
name: unit-tests-domain
description: Write or refactor domain-layer unit tests for DDD aggregates, domain services, and domain assert classes. Use for domain behavior coverage and test organization; excludes application, infrastructure, integration, and end-to-end tests.
---

# Domain Unit Testing

## Goal

The domain unit tests verifies the domain layer only in a BDD style.

## Scope and workflow

Inspect the domain model and existing domain unit test project before editing.

Inspect the domain model specification folder to understand the required behavior and verification tests. Use the specification as the sole source of truth for required behavior. Do not infer requirements from existing code, existing tests, or examples.

## Type of testing

Londen school of unit testing in a BDD style.
That means that the unit tests are written in a Given-When-Then style, with a common DSL behind the test suite. Avoid technical details. The unit tests must be readable and understandable by a non-technical domain expert. That means that all arrange, asserts, method invocations and mocks must be hidden behind a DSL respecting the ubiquitous language. The language used in the unit tests are selfdescriptive. The test reads like a story agreed upon by the domain experts and the developers.

## Files and folders

- The DSL layer is implemented in `UnitTest/Domain/Dsl/` and is shared across all domain unit tests. It contains the domain-specific test helpers, including the Given-When-Then DSL, the aggregate builders, and the domain assert classes.
- Put all domain unit tests in `UnitTests/Domain/`, relative to the existing test project layout. If the unit test project is named with a prefix, such as `Sales.UnitTests`, use its `Domain/` folder rather than creating a second test project.
- Bundle tests for each aggregate in `Domain/<AggregateName>/`.
- Use one separate `<AggregateName>Tests` file per aggregate. Keep all tests for that aggregate in that file.
- Use one separate `<DomainServiceName>Tests` file per domain service and put them in `Domain/DomainServices/`.
- Use one separate `<AssertClassName>Tests` file per domain assert class. Keep it in the relevant aggregate folder.
- Do not create separate test files or test classes for entities, value objects. Exercise their behavior through the owning aggregate's public operations in its aggregate test file.

Example layout:

```text
Some.Prefix.UnitTests/
└── Domain/
    ├── Order/
    │   ├── OrderTests.file-extension
    │   ├── OrderPricingServiceTests.file-extension
    │   └── OrderAssertsTests.file-extension
    └── Shared/
        ├── CurrencyConversionServiceTests.file-extension
        ├── MoneyAssertsTests.file-extension
        └── Mocks/
            └── ExchangeRateProviderMock.file-extension
```

The names are illustrative; create files only for domain types that exist and need coverage. An assert class means production domain logic, not a test assertion library or a test-only assertion helper.

## Rules

- Londen school of unit testing
- Readable for non-technical domain experts
- Test the aggregates
- Test the aggregate assert classes
- Test the domain services
- All domain layer unit test make use of the common DSL layer in `UnitTest/Domain/Dsl/` and the domain-specific test helpers, builders, and assert classes.
- Only the domain layer is tested.

## Exclusions

- Never test abstractions, they should be covered by the concrete implementations.
- Do not add application orchestration, persistence, repository implementation, infrastructure, or transport tests under this skill.
- Do not bypass aggregate boundaries with reflection, private-member access, or direct child mutation just to make assertions or setup easier.

## Validation

- Run the relevant domain unit tests using the repository's existing test command.
- The product specification must match the domain unit tests. If it doesn't, report the discrepancy and ask for clarification.
