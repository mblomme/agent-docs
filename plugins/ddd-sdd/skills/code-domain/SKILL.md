---
name: code-domain
description: Implement the domain layer in a TDD manner, writing the complete domain unit test suite based on the specification and implementing the domain layer to make the tests pass.
---

# Implement Domain

Follow the target project's programming language, toolchain, and conventions.
Express these object-oriented design rules using the language's supported
constructs; do not assume a particular framework, SDK, or test library.

Implement the domain layer in a TDD manner. First create the complete domain unit test suite based on the specification, then implement the domain layer to make the tests pass. The specification in `spec/` is the sole source of truth for required behavior unless it's for creating extra domain events.

## Pre-requisites

- Read the whole product specification.
- Use the `code-tests-domain` skill.
- If no specific scope is given, implement the whole domain layer. If a specific scope is given, implement only the specified part of the domain layer.

## Steps

1. Build a traceability table with a specification path and section or rule ID, required behavior to keep track of your progress.
2. Mark in this table what is already tested and implemented.
3. Write the domain unit test first based on the specification by using the `code-tests-domain` skill. Don't implement any code yet, the goals is to make the failing unit tests pass after all the unit tests are written.
4. Import statement should not be added while writing the unit tests, they should be added when implementing the domain layer to make the unit tests pass. Because you cannot guess the location of the domain layer classes and methods before implementing them.
5. Implement the domain layer according to the specification and the unit tests.
6. On contradictions between the code and the unit tests suite, the specification is the source of truth. Correct the code or the unit tests to match the specification.
7. When finnished, go to done section and verify that all the rules are tested and implemented.

## Done

This skill is only done when all of the following are true:

- Verify the tracability table matches the domain model specification.
- A carefull check is done to verify the domain model specification with the domain unit tests and the implemented domain layer.
- The traceability table is complete and all rules are tested and implemented.
- No verification rules remain untested.
- The domain layer unit test suite is complete and all tests pass.
- Only the domain layer is implemented or contains changes.
