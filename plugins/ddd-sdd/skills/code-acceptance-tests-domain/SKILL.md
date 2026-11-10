---
name: code-acceptance-tests-domain
description: Write or refactor domain-layer acceptance tests with unit tests.
---

# Domain Acceptance Tests

## GOAL

For all the aggregates and domain services involved in the scope and documented in the `/spec/product/domain`, write unit test that read as gherkin steps. 
The tests will fail, but another agent will make them pass by implementing the domain layer.

## PREREQUISITES

- Make sure there is a folder `Domain` in the UnitTests project.
- Restrict to the provided scope, if no scope is provides this means the whole domain layer.

## DESCRIPTION

Before any concrete implementation can be written, the domain model specification needs to be translated into acceptance unit tests that read like gherkin language.
The unit tests should compile. Accomplish this by delegating all implementation calls to a DSL facade. 
These parameterized functions in the dsl facade will throw a not implemented exception by default. 
The exception message will include a link to a specification document from the spec folder or even a concrete section in the spec.

Each testable component needs it's own dsl facade.
Another agent will implement the domain layer and import components of the src folder into the dsl to make the acceptance tests pass. 
Each scenario step is a function in the dsl.

The goals is that the other agent can make the tests pass by importing the domain components and calling their behavior through the DSL facade.

The other agent will not have access to the unit tests but only to the DSL to prevent creating false positives.

Invoking the other agent is beyond scope.


## PARAMETERIZED FUNCTIONS

Although every step function is a descriptive function in the dsl facade, parameterized functions are preferred. 
This improves parameterized unit tests, optimize for this.

Some examples, but do not limit to these examples:
- create `Given_minutes_capacity(int minutes)` instead of `Given_sixty_minutes_capacity()`
- create `When_minutes_of_eligible_work_is_accepted(int minutes)` instead of `When_90_minutes_of_eligible_work_is_accepted();
- etc ...


## RULES

- Always relative date and time values to the current date and time, never use absolute values.
- The acceptance unit test can only import the DSL facade or other testing libraries
- The initial dsl facade is a skeleton throwing not implemented exceptions with spec links or sections in spec documents.
- Only write code in the Domain folder of the UnitTests project.
- Make use of the full Gherkin syntax("Given, When, Then, And, But, Background, Scenario, ...") to describe test scenarios.
- Each scenario should be a separate unit test.
- Each unit test function has a descriptive name.
- Dsl function names are self descriptive.
- Optimize for parameterized functions in the dsl facade.
- `Then (assert) functions` really need parameters, good unit tests assert against hardcoded values, so the agent cannot create false possitives.
- The parameter names are self descriptive.
