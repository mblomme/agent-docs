---
name: code-domain
description: Implement the domain layer, based on the domain model specification.
---

# Implement Domain

Follow the target project's topology and technology.

The specification in `spec/` is the sole source of truth for required behavior unless it's for creating extra domain events.


## Useful skills

The following skills are useful for implementing the domain layer components:

- The tactical pattern skills.

## Pre-requisites

- Read the product specification.
- If no specific scope is given, implement the WHOLE domain layer.
- The acceptance tests of the domain layer are already written.

## Steps

1. Build a traceability table with a specification path and section or rule ID, required behavior to keep track of your progress.
2. Implement the domain layer.
3. Run the whole test suite, ALL THE TESTS MUST PASS.
3. When the domain layer acceptance test fail, follow section `Domain Acceptance Tests`.
4. On contradictions between the code and the unit tests suite, the specification is the source of truth.
5. When finnished, go to done section and verify that all the rules are tested and implemented.

## Domain Acceptance Tests

Do no write unit tests for the domain layer, the acceptance tests are already written. You are not allowed to alter the unit tests, make them pass by implementing the domain layer and using the implemented components in the DSL facade.

### Domain Layer Acceptance Tests rules
- It's forbidden to change the unit tests, they are already written and should be used as is.
- Make the domain layer acceptance tests pass by implementing the domain layer and using the implemented components in the DSL facade.
- The skeleton of the DSL facade is already written, you are not allowed to change it.

## Done

You are only done when all of the following are true:

- Run the whole test suite, ALL THE TESTS MUST PASS. Don't be foolded test output could be truncated, check the test report for all the tests and their results.
- Verify that all the created or updated domain classes and methods are according to the rules of the relevant domain skills.
- Verify the tracability table matches the domain model specification.
- A carefull check is done to verify the domain model specification with the domain layer acceptance tests and the implemented domain layer.
- The traceability table is complete and all the rules are tested and implemented.
