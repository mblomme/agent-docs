---
name: unit-tests-domain
description: Write or refactor domain-layer unit tests for DDD aggregates, domain services, and domain assert classes. Use for domain behavior coverage and test organization; excludes application, infrastructure, integration, and end-to-end tests.
---

# Domain Unit Testing

## Goal

The domain unit tests verifies the domain layer only in a BDD style.

## Prerequisites

- Read the product specification to understand the domain model.
- Read the technical specification to understand folder structure.
- Read the DesignTests suite to discover the location of the tactical ddd patterns.

## Scope and workflow

Inspect the domain model specification folder to understand the required behavior and verification tests. Use the specification as the sole source of truth for required behavior. Do not infer requirements from existing code, existing tests, or examples.

## Type of testing

Unit testing in BDD style.
That means that the unit tests according to BDD, with a common DSL behind the test suite. Avoid technical details. The unit tests must be understandable by a non-technical domain expert. That means that all arrange, asserts, method invocations and mocks must be replaces by human readable alternative available a DSL respecting the ubiquitous language. 

The unit tests reads like a sequence of steps written in the language of the domain experts and according to the ubiquitous language and builds up a story. Don't hide steps, just like a domain expert would not hide steps when explaining a process.

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
- Only unit tests for the domain layer are written.


## Exclusions

- Never test abstractions, they should be covered by the concrete implementations.
- Do not add application orchestration, persistence, repository implementation, infrastructure, or transport tests under this skill.
- Do not bypass aggregate boundaries with reflection, private-member access, or direct child mutation just to make assertions or setup easier.

## Validation

- Run the relevant domain unit tests using the repository's existing test command.
- The product specification must match the domain unit tests. If it doesn't, report the discrepancy and ask for clarification.
