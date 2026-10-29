---
name: code-tests-domain
description: Write or refactor domain-layer unit tests for DDD aggregates, domain services, and domain assert classes. Use for domain behavior coverage and test organization; excludes application, infrastructure, integration, and end-to-end tests.
---

# Domain Unit Testing

Follow the target project's programming language, toolchain, and conventions.
Express these object-oriented design rules using the language's supported
constructs; do not assume a particular framework, SDK, or test library.

## Goal

The domain unit tests verifies the domain layer only in a BDD style.

## Prerequisites

- Read the product specification to understand the domain model.
- Read the technical specification to understand folder structure.

## Scope and workflow

Inspect the domain model specification folder to understand the required behavior and verification tests. Use the specification as the sole source of truth for required behavior. Do not infer requirements from existing code, existing tests, or examples.

## Type of testing

Unit testing in BDD style.
That means that the unit tests are according to BDD, with a common DSL behind the test suite. Avoid technical details. The unit tests must be understandable by a non-technical domain expert. That means that all technical details must be replaced by human readable alternative available a DSL respecting the ubiquitous language. 

The unit tests reads like a sequence of steps written in the language of the domain experts and according to the ubiquitous language and builds up a story. Don't hide steps, just like a domain expert would not hide steps when explaining a process.

## Files and folders

- The DSL layer is implemented in `UnitTests/Domain/Dsl/` and is shared across all domain unit tests. It contains the domain-specific test helpers, including the Given-When-Then DSL, the aggregate builders, and the domain assert classes.
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
    ├── Dsl/
    ├── DomainServices/
    │   ├── OrderPricingServiceTests.file-extension
    │   └── CurrencyConversionServiceTests.file-extension
    ├── Order/
    │   ├── OrderTests.file-extension
    │   └── OrderAssertsTests.file-extension
    └── Shared/
        ├── MoneyAssertsTests.file-extension
        └── Mocks/
            └── ExchangeRateProviderMock.file-extension
```

The names are illustrative; create test files only for domain types that exist and need coverage, and DSL files as needed to support those tests. An assert class means production domain logic, not a test assertion library or a test-only assertion helper.

## Rules

- Londen school of unit testing
- Comply to the BDD rules and best practices below
- Add a white space between the logical sections of the unit test to improve readability.
- Readable for non-technical domain experts
- Test the aggregates
- Test the aggregate assert classes
- Test the domain services
- All domain layer unit test make use of the common DSL layer in `UnitTests/Domain/Dsl/` and the domain-specific test helpers, builders, and assert classes.
- Only unit tests for the domain layer are written.

## BDD rules and best practices

1. Start with behavior
- Describe **what the system should do and why it matters**, not how it is implemented.
- Use the domain’s shared language consistently.

2. Write focused scenarios
- Give each scenario a descriptive, business-oriented name.
- Cover one behavior or business rule per scenario.
- Make the actor, relevant context, and expected outcome clear.
- Keep scenarios independent and understandable without reading helper code.

3. Use Given–When–Then
- **Given:** Establish the relevant starting context.
- **When:** Describe the action or event being examined.
- **Then:** Verify the observable outcome.
- Use **And** for additional context or related outcomes when it improves readability.
- Avoid putting the behavior under test in a `Given` merely to make the scenario fit the format.

4. Handle AND and OR deliberately
- **AND:** Multiple conditions or outcomes can belong in one scenario when they describe the same behavior.
- **OR:** Usually give each meaningful alternative its own scenario, so failures clearly identify which case broke.
- For combined rules, include relevant success cases, failure cases, and boundaries—not necessarily every possible combination.
- Multiple assertions are acceptable when they verify related consequences of the same action.

5. Make the outcome specific
- Check observable state or results, including that rejected actions leave important state unchanged.
- Verify the relevant reason for rejection where it is a stable part of the domain contract; a generic exception alone may not prove the intended rule caused it.
- Avoid assertions about incidental implementation details.

6. Keep scenarios useful and reliable
- Use concrete, meaningful examples, including boundary values.
- Keep helpers readable; don’t hide important business conditions inside them.
- Make tests deterministic and isolated, including control over time and external dependencies.
- Use parameterized examples when they express the same rule clearly; avoid combining unrelated behaviors for brevity.
- BDD does **not** require Gherkin or a particular framework. Given–When–Then is a communication aid, not the goal itself.


## Exclusions

- Never test abstractions, they should be covered by the concrete implementations.
- Do not add application orchestration, persistence, repository implementation, infrastructure, or transport tests under this skill.
- Do not bypass aggregate boundaries with reflection, private-member access, or direct child mutation just to make assertions or setup easier.

## Validation

- Run the relevant domain unit tests using the repository's existing test command.
- The product specification must match the domain unit tests. If it doesn't, report the discrepancy and ask for clarification.
