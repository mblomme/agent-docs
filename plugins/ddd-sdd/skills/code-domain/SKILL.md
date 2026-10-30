---
name: code-domain
description: Implement the domain layer in a TDD manner, by writing the complete domain unit test suite first based on the specification and implementing the domain layer to make the tests pass.
---

# Implement Domain

Follow the target project's topology and technology.

Implement the domain layer in a TDD manner. First create the complete domain unit test suite based on the specification, then implement the domain layer to make the tests pass. The specification in `spec/` is the sole source of truth for required behavior unless it's for creating extra domain events.

## Useful skills

The following skills are useful for implementing the domain layer components:

- code-aggregate
- code-entity
- code-repository
- code-domain-event
- code-value-object
- code-domain-service
- code-tests-domain

## Pre-requisites

- If no specific scope is given, implement the whole domain layer.
- Read the product specification.

## Steps

1. Build a traceability table with a specification path and section or rule ID, required behavior to keep track of your progress.
2. Mark in this table what is already tested and implemented.
3. First, write all the domain unit test based on the specification by using the `code-tests-domain` skill. Don't implement any code yet, the goals is to make the failing unit tests pass after all the unit tests are written.
5. Secondly, implement the domain layer by making the unit tests pass AND using the appropriate domain skills (the location and construction of the domain components cannot be deduced from the unit tests).
6. On contradictions between the code and the unit tests suite, the specification is the source of truth. Correct the code or the unit tests to match the specification.
8. All created domain layer components are created or updated according 
7. When finnished, go to done section and verify that all the rules are tested and implemented.

## Done

You are only done when all of the following are true:

- Run the whole test suite, ALL THE TESTS MUST PASS. Don't be foolded test output could be truncated, check the test report for all the tests and their results.
- Verify that all the created or updated domain classes and methods are according to the rules of the relevant domain skills.
- Verify the tracability table matches the domain model specification.
- A carefull check is done to verify the domain model specification with the domain unit tests and the implemented domain layer.
- The traceability table is complete and all the rules are tested and implemented.
