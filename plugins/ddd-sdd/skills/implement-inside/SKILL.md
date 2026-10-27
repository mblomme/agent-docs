---
name: implement-inside
description: Implement the inside of a hexagonal architecture from the specification in order, using implement-domain first and implement-application second, including their unit tests and specification verification. Excludes infrastructure, adapters, and host implementation.
---

# Implement Inside

Implement the inside layers in dependency order: **Domain, then Application**.
Use the existing layer skills to execute the work, including their complete unit
test suites and comparisons against the specification. The documented `spec/`
is the sole source of truth for required behavior.

## 1. Establish the shared scope

- Read repository instructions and locate the specification folder. Establish
  the target bounded context and requested functionality from the request and
  specification. Inspect existing code and test tooling; preserve unrelated work.
- Read applicable specification indexes, product requirements, use cases, and
  architecture, technology, and testing rules. Follow references and declared
  dependencies. Do not invent requirements or rewrite the specification to
  accommodate code.
- Locate implementation skills for the domain and application layer.
- Carry the same repository, bounded context, specification references, and
  requested scope through both phases. Maintain requirement-to-code-to-test
  traceability across the layers using the layer skills' tables.

## 2. Complete the domain first

Execute `implement-domain` for the established scope. Implement or reconcile the
domain, write its complete unit test suite, and complete its verification against
the specification. Reuse correct existing code; an existing domain still needs
the skill's inspection and verification.

Do not begin dependent application implementation until the required domain
behavior is implemented, its unit suite passes, and no known specification
mismatch remains. Missing requirements, failed checks, or unavailable verification
must remain explicit blockers rather than being treated as successful completion.

Carry the verified public domain contracts, test results, and relevant findings
into the application phase. Do not stop after the domain phase when the
application phase can proceed.

## 3. Implement the application second

Execute `implement-application` against the verified domain and the same scope.
Complete its application implementation, unit tests, and specification comparison.

If application work reveals a domain issue, use `implement-application`'s
structured extra prompt to execute `implement-domain` again with the issue,
specification evidence, observed behavior, application impact, and acceptance
criteria. Verify the domain correction before resuming dependent application
work, then reconcile changed contracts and rerun affected application tests.
Do not move business rules into the application to bypass a domain defect.
If an issue repeats without new evidence, report the blocker instead of cycling
between skills indefinitely.

## 4. Verify the completed inside

- Confirm every in-scope requirement is implemented in the correct layer and
  covered by meaningful tests grounded in the specification. Check for omissions,
  unsupported behavior, duplicated business rules, and incompatible contracts.
- Verify that dependencies point inward: Domain does not depend on Application;
  neither layer depends on Infrastructure or Main. Implement only the specified
  inside-owned ports and contracts; leave their external adapters outside scope.
- Confirm the final code passes both complete unit suites and applicable build,
  formatting, static analysis, and architecture checks. Reuse results for the
  unchanged final code from the layer skills; rerun checks invalidated by later
  edits. Do not claim real adapter or database behavior from mocked tests.
- Keep infrastructure, transport, persistence implementations, host startup,
  and deployment outside this workflow. Supporting project changes are limited
  to those needed to build and test the inside layers.

Report the completed domain and application scope, specification traceability,
any domain repair prompted by application work, verification results, and
remaining gaps or blockers. Claim completion only when both phases and their
required verification are complete.
