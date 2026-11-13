---
name: build-acceptance-tests-domain
description: Write or refactor domain-layer acceptance tests using Gherkin features, step definitions, and a DSL Driver Facade in the target project's language and test framework.
---

# Domain Acceptance Tests (Gherkin + DSL Driver Pattern)

## Goal

Translate the aggregates, entities, value objects, and domain services in the provided scope, as documented in `/spec/product/domain`, into executable Gherkin acceptance tests, step definitions, DSL contracts, and adapter skeletons.

The test suite must compile or load successfully, discover the scenarios, and fail explicitly at unimplemented adapter methods with links to the relevant specification sections. A later domain implementation workflow will implement the domain and wire the adapters to make these tests pass. Invoking that workflow is out of scope.

## Prerequisites and Scope

- Read the domain specification and restrict work to the requested scope. If none is provided, cover the entire documented domain layer.
- Inspect the target repository's language, test framework, domain test location, dependency manifests, and build/test commands before choosing file names or APIs.
- Reuse the existing Gherkin runner, assertion library, and test conventions. Apply an available technology-specific acceptance-test skill when it matches the target stack; keep framework setup and syntax in that companion skill.
- If no Gherkin runner is configured, choose one compatible with the project's language and toolchain, following repository guidance or explicit user choices. Add only the test dependencies and configuration needed to discover and execute the features.
- Keep changes within the domain test area and narrowly necessary test dependency/build configuration. Do not implement or modify production domain, application, or infrastructure code, or change the specifications.

## Architecture and Artifact Layout

Resolve actual paths, capitalization, file extensions, and module names from repository conventions. The following layout illustrates responsibilities rather than prescribing a project name or language:

```text
<domain-test-root>/
├── features/
│   └── <component>.feature
├── steps/
│   ├── <shared-domain-steps>
│   └── <component-steps>
├── dsl/
│   ├── contracts/
│   │   └── <component-dsl-contract>
│   └── adapters/
│       └── <component-dsl-adapter>
└── support/
    ├── <scenario-context>
    └── <adapter-registration-or-factory>
```

Treat features, step definitions, DSL contracts, and shared scenario-context semantics as read-only for the later domain implementation workflow. Adapter skeletons are the intended implementation seam; registration may later change as needed to wire real domain dependencies.

### Prevent False Positives

- Put acceptance assertions in `Then` step definitions, never in adapters.
- Have inspection methods return observed values, data records, or domain error states. Compare these with expected values supplied by the scenario.
- Keep expected values out of adapter inspection calls. Adapters must eventually observe real domain behavior, not return expected constants or recreate business calculations.
- Ensure unimplemented adapter calls fail the scenario. Do not use pending, skipped, ignored, or successful no-op behavior as a substitute for failure.

## Workflow

### 1. Write Gherkin Features

- Use the Ubiquitous Language from the specification and declarative business steps.
- Cover the in-scope rules, valid behavior, invariant failures, and boundaries. Track each scenario to its specification file and section anchor or rule ID.
- Use the Gherkin constructs supported by the selected runner where they clarify the specification: `Feature`, `Rule`, `Background`, `Scenario`, `Scenario Outline`, `Examples`, `Given`, `When`, `Then`, `And`, and `But`.
- Prefer parameterized steps and `Scenario Outline` with `Examples` for variations and boundary cases.
- Keep one feature-to-DSL-contract-and-adapter mapping unless cross-aggregate interactions require a shared facade.

### 2. Write Step Definitions

- Use the selected runner's native binding mechanism: annotations, decorators, registration functions, or the repository's equivalent.
- Bind parameters with the runner's supported expressions or patterns and appropriate conversions.
- Obtain the DSL facade through the project's scenario-scoped dependency mechanism, fixture, or factory. Do not require a particular dependency-injection container or constructor syntax.
- Delegate setup and actions from `Given` and `When` steps to the DSL.
- Query observed state, results, errors, or events in `Then` steps and assert against scenario expectations using the project's assertion library.
- Keep domain implementation types out of step definitions. Use DSL contracts, test-owned data types, standard library types, and test framework facilities.
- Name step functions descriptively after their business behavior; do not use numbered names such as `Step1` or `Step2`.

### 3. Define DSL Contracts

- Express setup, actions, and inspection separately, using interfaces, protocols, abstract types, or another explicit callable contract idiomatic to the language.
- Use standard scalar, collection, date/time, and duration types, or test-owned data records and error representations. Do not expose production domain types.
- Keep contracts and bindings usable before domain classes exist; they must not import or require production domain modules.
- Parameterize actors, identifiers, quantities, dates, and other inputs. Do not encode scenario values or persona names in method names.

For example, using language-neutral pseudocode:

```text
Given a daily capacity of 60 minutes
  bind "a daily capacity of <integer> minutes"
  call dsl.setDailyCapacity(capacityInMinutes)

When 30 minutes of eligible work is accepted
  bind "<integer> minutes of eligible work is accepted"
  call dsl.acceptEligibleWork(durationInMinutes)

Then the remaining capacity should be 30 minutes
  bind "the remaining capacity should be <integer> minutes"
  assertEqual(dsl.remainingCapacityInMinutes(), expectedRemainingMinutes)
```

Avoid methods such as `givenSixtyMinutesCapacity()` or `changeBobsZone(zone)`. Pass values and actor identifiers as parameters. Avoid assertion-shaped adapter methods such as `thenRemainingCapacityIs(expectedMinutes)`.

### 4. Create Adapter Skeletons and Scenario Support

- Implement each DSL contract as an adapter skeleton and register or construct it using the selected runner's scenario lifecycle.
- Make every contract method fail immediately with the language's explicit not-implemented exception or equivalent test failure, including an exact specification reference such as `Spec: /spec/product/domain/<document>.md#<section-anchor>`.
- Do not fabricate state, return default values, or implement domain behavior in skeletons.
- Keep scenario context isolated between scenarios while sharing it among collaborating adapters within a scenario.
- Define how bindings inspect expected domain failures using an error result or scenario-local captured failure. Unexpected errors and not-implemented failures must propagate as test failures, never become accepted business outcomes.
- Leave framework-generated bindings, wrappers, or code-behind files to the runner's supported generation process. Author only the source artifacts it expects.

## Relative Dates and Times

Express dates and times relative to a scenario reference anchor, such as `today`, `tomorrow`, `in 2 days`, or `30 minutes ago`. Resolve them in step definitions or shared scenario context against a frozen test clock. Do not read the live wall clock independently in bindings or adapters.

For example: `Given a day plan for "today + 1 day" with a capacity of 480 minutes`.

Use the target language's date, time, duration, and time-zone representations with explicit units and zone semantics where relevant.

## Verification

Use the repository's build, compilation, type-checking, or module-load checks as appropriate for its language, followed by its acceptance-test command. Do not prescribe a particular CLI.

Before finishing:

- Confirm the test suite compiles or loads without errors and all in-scope scenarios and Scenario Outline examples are discovered.
- Verify every `Given`, `When`, `Then`, `And`, and `But` step has exactly one applicable binding, including steps after the first failing adapter call. Use the runner's dry-run or binding validation when available; otherwise cross-check all feature steps against registered bindings, accounting for inherited step keywords and parameter conversions. A normal test run alone may miss undefined later steps because execution stops at the first failure.
- Confirm newly scaffolded scenarios fail with the intended specification-linked not-implemented failures. Undefined or ambiguous bindings, pending or skipped scenarios, configuration errors, and zero discovered tests do not satisfy this requirement.
- For refactoring existing tests, preserve already implemented behavior and passing scenarios; apply the expected-failure requirement to new adapter skeletons.
- Reconcile feature coverage with every in-scope specification rule, including failure and boundary cases.
- Inspect the diff for domain coupling, assertions in adapters, unintended production changes, and hand-authored generated files.

Finish with a concise report of covered scope, artifact locations, selected language and framework, verification commands and results, intentional skeleton failures, and any coverage or environment blockers. Do not claim successful verification for checks that could not be run.
