---
name: build-acceptance-tests-domain
description: Write or refactor domain-layer acceptance tests using Reqnroll Gherkin .feature files, Step Definitions, and a DSL Driver Facade.
---

# Domain Acceptance Tests (Reqnroll + DSL Driver Pattern)

## GOAL

For all aggregates, entities, value objects, and domain services involved in the provided scope and documented in `/spec/product/domain`, write executable **Reqnroll Gherkin acceptance tests** (`.feature` files, `[Binding]` Step Definitions, and a DSL Driver Facade).
The project must compile cleanly and the tests must fail with a `NotImplementedException` pointing to the relevant specification section. Another agent will later make them pass by implementing the domain layer and wiring the DSL Driver Facade.

## PREREQUISITES

- Ensure `Reqnroll.xUnit` is installed in `tests/Simplifyme.FocusFlow.UnitTests` and there is a `Domain` folder in the `UnitTests` project.
- `Reqnroll` MSBuild code-behind generation must be enabled in the test project.
  -  Set `ReqnrollUseIntermediateOutputPathForCodeBehind` to `true`
- Restrict work to the provided scope; if no scope is provided, cover the entire domain layer documented in `/spec/product/domain` and nothing less.

## ARCHITECTURE & FOLDER STRUCTURE

All domain acceptance test artifacts live under `tests/Simplifyme.FocusFlow.UnitTests/Domain`:

```text
tests/Simplifyme.FocusFlow.UnitTests/Domain/
├── Features/                  # Owned by THIS agent (Read-only for Implementation Agent)
│   └── <Component>.feature
├── StepDefinitions/           # Owned by THIS agent (Read-only for Implementation Agent)
│   ├── CommonDomainSteps.cs
│   └── <Component>Steps.cs
├── Dsl/
│   ├── Contracts/             # Owned by THIS agent (Read-only for Implementation Agent)
│   │   └── I<Component>Dsl.cs
│   └── Adapters/              # Stubbed by THIS agent; implemented by Implementation Agent
│       └── <Component>Dsl.cs
└── Support/                   # Shared test infrastructure
    ├── DomainTestContext.cs
    └── DslDependencies.cs

```

### How False Positives Are Prevented

1. **`.feature` files, `StepDefinitions/`, and `Dsl/Contracts/` are locked (read-only)** for the Implementation Agent.
2. **All assertions (`Should().Be(...)`) MUST live inside `[Then]` Step Definitions**, never inside the `Dsl/Adapters/` classes.
3. `I<Component>Dsl` inspection methods must return actual values (primitives, DTOs, or domain error states) to the `[Then]` Step Definitions, which compare them against the expected parameters from the Gherkin step. This makes it impossible for the Implementation Agent to pass tests with empty methods.

## DESCRIPTION

Before any concrete domain implementation is written, translate the domain model specification in `/spec/product/domain` into Gherkin `.feature` files, Reqnroll `[Binding]` Step Definitions, and a per-component DSL Facade (`I<Component>Dsl` + `<Component>Dsl`).

1. **Gherkin Feature Files (`Features/<Component>.feature`):**
* Use full Gherkin 6+ syntax (`Feature`, `Rule`, `Background`, `Scenario`, `Scenario Outline`, `Examples`, `Given`, `When`, `Then`, `And`, `But`).
* Write declarative business steps in the Ubiquitous Language from `/spec/product/domain`.


2. **Step Definitions (`StepDefinitions/<Component>Steps.cs`):**
* Decorate classes with `[Binding]` and inject `I<Component>Dsl` via the primary constructor.
* Use Reqnroll Cucumber Expressions (`{int}`, `{string}`, `{decimal}`) to bind parameterized steps.
* `[Given]` and `[When]` steps delegate state setup and actions to `I<Component>Dsl`.
* `[Then]` steps query state from `I<Component>Dsl` and perform assertions against the Gherkin step parameters.


3. **DSL Facade Contract (`Dsl/Contracts/I<Component>Dsl.cs`):**
* Each testable aggregate or domain service gets its own DSL interface.
* **Never reference types from `Simplifyme.FocusFlow.Domain**` in `StepDefinitions` or `I<Component>Dsl`. Use only primitive C# types (`int`, `string`, `bool`, `decimal`, `TimeSpan`, `DateTimeOffset`) so the test project compiles before the domain classes exist.


4. **DSL Facade Skeleton (`Dsl/Adapters/<Component>Dsl.cs`):**
* Implements `I<Component>Dsl` and is registered in Reqnroll's DI container (`Support/DslDependencies.cs`).
* Every method in `<Component>Dsl` initially throws `new NotImplementedException("See /spec/product/domain/<file>.md#<section>")` referencing the exact specification file and section.
* Invoking the Implementation Agent is out of scope for this skill.



## PARAMETERIZED FUNCTIONS & STEPS

Always prefer parameterized Gherkin steps and parameterized DSL methods over hardcoded variants. Use `Scenario Outline` + `Examples` when testing boundary rules.

**Good (Parameterized):**

* Gherkin: `Given a daily capacity of 60 minutes` -> Binding: `[Given("a daily capacity of {int} minutes")]`
* DSL Contract: `void SetDailyCapacity(int capacityInMinutes);`
* Gherkin: `When 90 minutes of eligible work is accepted` -> Binding: `[When("{int} minutes of eligible work is accepted")]`
* DSL Contract: `void AcceptEligibleWork(int durationInMinutes);`
* Gherkin: `Then the remaining capacity should be 30 minutes` -> Binding: `[Then("the remaining capacity should be {int} minutes")]`
* DSL Contract: `int GetRemainingCapacityInMinutes();` (asserted in Step Definition: `dsl.GetRemainingCapacityInMinutes().Should().Be(expectedRemainingMinutes);`)

**Bad (Hardcoded / Vulnerable to False Positives):**

* `void Given_sixty_minutes_capacity();` (Hardcoded value in method name)
* `void Then_remaining_capacity_is(int minutes);` (Bad if the assertion happens inside the DSL instead of the Step Definition!)

## RELATIVE DATES & TIMES

Never use hardcoded absolute calendar dates (e.g., `"2026-11-10"`). Always express dates and times relative to a reference anchor (`today`, `tomorrow`, `in 2 days`, `at +09:00`, `30 minutes ago`) and resolve them inside the Step Definition or `DomainTestContext` relative to a deterministic clock (`DateTimeOffset.UtcNow` or a frozen test clock).

* *Good Gherkin:* `Given a day plan for "today + 1 day" with a capacity of 480 minutes`
* *Bad Gherkin:* `Given a day plan for "2026-11-10" with a capacity of 480 minutes`

## RULES

- **Strict Scope & Location:** Only create or modify files inside `tests/Simplifyme.FocusFlow.UnitTests/`. Never modify `src/**` or `lib/**`.
- **Zero Domain Coupling in Tests:** `StepDefinitions` and `Dsl/Contracts` may only import `Dsl/Contracts`, standard .NET types, `Reqnroll`, and testing assertion libraries (`FluentAssertions` / `Xunit`). They must NEVER import `Simplifyme.FocusFlow.Domain`.
- **Spec-Linked Skeletons:** Every method in `Dsl/Adapters/<Component>Dsl.cs` must initially throw:
`throw new NotImplementedException("Spec: /spec/product/domain/<document>.md#<section-anchor>");`
- **Full Gherkin Expressiveness:** Use `Feature`, `Rule`, `Background`, `Scenario`, `Scenario Outline`, `Examples`, `Given`, `When`, `Then`, `And`, and `But` to mirror the structure of `/spec/product/domain`.
- **Assertions Stay in `[Then]` Steps:** `[Then]` steps must always take expected parameters from Gherkin and assert against values returned by `I<Component>Dsl` getter/query methods (or `DomainTestContext.LastException` for domain invariant failures).
- **Self-Descriptive Naming:** Use clear, Ubiquitous Language names for `.feature` scenarios, Step Definition methods, DSL contract methods, and all method parameters.
- **Verification Gate:** Before finishing, run `dotnet build` to guarantee 0 compilation errors, and run `dotnet test` to verify the newly generated scenarios execute and fail with the expected `NotImplementedException` spec links.
- **Never generate `.feature.cs` files manually:** Reqnroll's MSBuild generator creates code-behind files automatically during `dotnet build`. Only write `.feature` files.
- **Forbid numbered step method names:** Never name step definition methods `Step1`, `Step2`, etc. Name every `[Binding]` method descriptively after the Gherkin text (e.g., `WhenFocusBlockIsSaved`, `ThenPlannedItemCountShouldBe`).
- **Forbid persona names in DSL method names:** Never hardcode actor names like `Alice` or `Bob` into `I<Component>Dsl` method names (e.g., never write `ChangeBobsZone(string actor, string zone)` or `AcceptBobsInvitation(string actor)`). Always pass both actors as parameters: `ChangePreferredZone(string actor, string targetOwner, string zone)`.
- **1-to-1 Feature-to-DSL mapping:** Create exactly one `I<Feature>Dsl` interface and one `<Feature>Dsl` adapter per `.feature` file unless testing cross-aggregate interactions. Ensure every Gherkin line in `.feature` has a matching `[Given]`/`[When]`/`[Then]` attribute in `StepDefinitions/`.
- **Verify 100% Step Binding Coverage:** Because Reqnroll stops executing a scenario on the first `NotImplementedException` in a `Given` step, unbound `When` or `Then` steps will not be detected by `dotnet test` alone. Before finishing, cross-check every single `Given`, `When`, `Then`, `And`, and `But` line in every `.feature` file against your `[Given]`/`[When]`/`[Then]` attributes in `StepDefinitions/` to guarantee zero undefined steps.
- **When done**, verify that the whole scope is covered by the generated `.feature` files, that all scenarios fail with `NotImplementedException` pointing to the correct spec section, and that the project compiles cleanly.