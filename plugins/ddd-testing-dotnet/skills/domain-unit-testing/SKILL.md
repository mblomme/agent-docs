---
name: domain-unit-testing
description: Write or refactor C# and .NET domain-layer unit tests for DDD aggregates, domain services, and domain assert classes. Use for domain behavior coverage and test organization; excludes application, infrastructure, integration, and end-to-end tests.
---

# Domain Unit Testing

## Scope and workflow

Inspect the domain model and existing unit test project before editing. Reuse the project's test framework, assertion library, mocking library, and naming conventions. Identify aggregate ownership, domain services, and domain assert classes (classes that enforce domain assertions or guards).

Test observable business behavior through public domain APIs: valid transitions, rejected operations, boundary conditions, invariant preservation, and emitted domain events where relevant. Keep tests deterministic and independent of databases, networks, files, and infrastructure. Use real domain objects where practical and mock only domain-owned dependencies needed by the behavior.

## Files and folders

- Put all domain unit tests in `UnitTests/Domain/`, relative to the existing test project layout. If the unit test project is named with a prefix, such as `Sales.UnitTests`, use its `Domain/` folder rather than creating a second test project.
- Bundle tests for each aggregate in `Domain/<AggregateName>/`.
- Use one separate `<AggregateName>Tests.cs` file per aggregate. Keep all tests for that aggregate in that file.
- Use one separate `<DomainServiceName>Tests.cs` file per domain service. Place it in the relevant aggregate folder when it belongs to that aggregate. Put cross-aggregate services in `Domain/Shared/`.
- Use one separate `<AssertClassName>Tests.cs` file per domain assert class. Keep it in the relevant aggregate folder, or `Domain/Shared/` when shared across aggregates.
- Do not create separate test files or test classes for entities, including domain entities and aggregate-owned child entities. Exercise their behavior through the owning aggregate's public operations in its aggregate test file.
- Put all domain mocks, including reusable mock wrappers and hand-written dependency doubles, under `Domain/Shared/Mocks/`. Do not scatter mock classes into aggregate folders. Configure mocks in the Arrange section as needed.

Example layout:

```text
UnitTests/
└── Domain/
    ├── Order/
    │   ├── OrderTests.cs
    │   ├── OrderPricingServiceTests.cs
    │   └── OrderAssertsTests.cs
    └── Shared/
        ├── CurrencyConversionServiceTests.cs
        ├── MoneyAssertsTests.cs
        └── Mocks/
            └── ExchangeRateProviderMock.cs
```

The names are illustrative; create files only for domain types that exist and need coverage. An assert class means production domain logic, not a test assertion library or a test-only assertion helper.

## Arrange, Act, Assert

Every unit test must visibly follow the AAA pattern, with `// Arrange`, `// Act`, and `// Assert` sections:

- Arrange: construct domain objects and inputs, establish initial state through supported APIs, and configure required mocks.
- Act: perform the single operation under test. For rejection scenarios, capture a delegate for that operation and assert the exception in Assert using the project's assertion library.
- Assert: verify the expected business outcome, domain error, or events. For rejected mutations, verify relevant state remains valid. Verify dependency interactions only when they are part of the business contract.

Parameterized tests must follow the same structure for each case. Keep fixture helpers focused on setup so the behavior under test remains visible.

## Exclusions

- Never directly unit test tactical DDD pattern base classes, such as base aggregate roots, entities, value objects, domain events, repositories, or domain services. Cover inherited behavior only as observed through concrete business types; do not add base-class test files or dummy subclasses solely to test the framework.
- Do not add application orchestration, persistence, repository implementation, infrastructure, or transport tests under this skill.
- Do not bypass aggregate boundaries with reflection, private-member access, or direct child mutation just to make assertions or setup easier.

## Validation

Run the relevant domain unit tests using the repository's existing test command. Check the changed files against the folder, file ownership, AAA, mock placement, and base-class exclusions above. Report what ran and any blockers; do not claim passing tests that were not executed.
