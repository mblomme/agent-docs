---
name: code-tests-application
description: Write or refactor application-layer unit tests for use cases, command handlers, and query handlers in a hexagonal architecture. Exercise real domain behavior while mocking injected ports and other dependencies; excludes domain-only, infrastructure, integration, and end-to-end tests.
---

# Application Unit Testing

Follow the target project's programming language, toolchain, and conventions.
Express these object-oriented design rules using the language's supported
constructs; do not assume a particular framework, SDK, or test library.

## Scope and workflow

Inspect the application implementation, its contracts, the domain model it uses, and the existing unit test project before editing. Reuse the project's language, test framework, assertion library, mocking library, and naming conventions. Identify the application entry point, injected dependencies, unit-of-work contract, and specified success and failure outcomes.

Test observable application behavior through the public use case or handler API: input mapping, orchestration, authorization when specified, domain invocation, persistence requests, and result or error mapping. Keep business rules in the real domain model. Do not invent behavior, transaction semantics, or error contracts solely to write a test.

## Real domain and mocked dependencies

- Instantiate the real application class under test. Use real aggregates, entities, value objects, domain events, and domain behavior. Avoid mocking or partially mocking the domain model, including replacing aggregate methods with canned results.
- Construct valid domain fixtures through supported factories, constructors, and public operations. Mocked repositories must return real aggregates. Use real domain failures to exercise the application's handling of rejected business operations.
- Mock all injected driving and driven ports and any other injected dependency of the application class, including unit-of-work ports, repositories, gateways, event publishers, clocks, identity providers, and identifier generators where present. Do not use real infrastructure adapters, databases, networks, files, or external services.
- A driving port implemented by the application class is the entry point under test: invoke its real implementation through that contract. Mock a driving port when it is an injected collaborator called by the class under test. Never mock the operation being tested.
- When a domain service is itself an injected dependency, mock that dependency at the application boundary. Execute the real domain behavior reached through the actual domain objects; do not introduce mocks inside those objects merely to avoid running their logic. Cover an injected domain service's own business rules in domain tests.
- Configure dependency mocks to supply deterministic data and failures. Do not replace domain state or business outcomes with mock expectations. Prefer assertions on real domain state and application results, with interaction assertions for meaningful boundary effects.

## Unit of work and repositories

Follow the existing unit-of-work API and its ownership of transaction completion.

- When the unit of work accepts a callback, configure its mock to execute and await that callback, supplying mocked repositories or transaction-scoped ports as required by the real signature. A canned successful return that skips the callback does not exercise the use case.
- Return real aggregates from mocked repository lookups. Capture newly added aggregates or persistence arguments when needed to verify their identifiers and business state.
- Verify persistence and transaction requests only as required by the application's contract. Do not require the application to call commit or rollback when the unit of work owns completion.
- On rejection or dependency failure, verify relevant forbidden effects, such as no add, save, or publish after an earlier failure. Account for mutable arguments: capture state at invocation time when later mutation could make an assertion misleading.
- Test the application's response to transaction or persistence failures using mocks. Actual rollback, database constraints, and adapter behavior belong in integration tests.

## Coverage choices

Choose cases that distinguish application behavior:

- Successful creation or mutation: verify the real aggregate's resulting state, required persistence requests, and minimal returned result.
- Missing records and specified authorization failures: verify the expected application outcome and absence of subsequent effects.
- Domain rejection: arrange a real aggregate and input that cause the actual domain operation to reject, then verify the application's documented propagation or mapping and relevant absence of persistence effects.
- Dependency failures and cancellation: cover propagation, mapping, or cancellation forwarding where part of the contract. Do not add retries or exception handling to satisfy invented expectations.
- Queries, when present: verify request mapping, calls to mocked read ports, and returned projection mapping without invoking real read adapters.

Do not repeat every domain invariant or boundary case already covered by domain tests. Include enough real domain behavior to establish that application orchestration invokes the correct operation with the correct inputs. Avoid asserting internal helper calls, incidental call counts, or ordering unless the contract requires them.

## Files and folders

- Put application unit tests in `UnitTests/Application/`, relative to the existing test project layout. For a prefixed project such as `Sales.UnitTests`, use its `Application/` folder rather than creating a second test project.
- Use one `<ApplicationClassName>Tests` file per concrete use case or handler. Group files by feature when the existing application layout does so.
- Put reusable mock wrappers and hand-written dependency doubles under `Application/Shared/Mocks/`. Keep shared fixture builders and other setup helpers under `Application/Shared/`. Configure scenario-specific mock behavior in Arrange.
- Keep domain-only tests in the domain test area. Do not create tests for abstract application base classes or port interfaces in isolation; exercise their contracts through concrete application behavior.

Example layout:

```text
Some.Prefix.UnitTests/
└── Application/
    ├── CreateOrderTests.file-extension
    ├── ConfirmOrderTests.file-extension
    └── Shared/
        ├── OrderFixture.file-extension
        └── Mocks/
            ├── UnitOfWorkMock.file-extension
            └── OrderRepositoryMock.file-extension
```

Create only the files and helpers needed by actual application types and scenarios.

## Arrange, Act, Assert

Every unit test must visibly follow AAA with `// Arrange`, `// Act`, and `// Assert` sections, using equivalent comment syntax in the project's language:

- Arrange: create real domain fixtures and input DTOs, instantiate the real application class, and configure all dependency mocks, including execution of unit-of-work callbacks where applicable.
- Act: invoke one public application operation and await asynchronous work. For rejection scenarios, capture a delegate or equivalent callable and assert the exception in Assert using the project's assertion library.
- Assert: verify the returned outcome, real aggregate state, or documented error, and the required or forbidden effects at mocked boundaries.

Parameterized tests must keep the same structure for each case. Helpers should simplify setup while leaving the operation and expected behavior visible.

## Validation

Run the relevant application unit tests using the repository's existing command. Check that the real application operation and domain behavior execute, all injected collaborators are mocked, asynchronous callbacks are awaited, and tests follow the folder and AAA conventions. Report what ran and any blockers; do not claim passing tests that were not executed.
