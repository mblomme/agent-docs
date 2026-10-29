---
name: code-application
description: Implement the application layer only from the repository's specification, assuming the domain is already implemented. Write application unit tests, verify behavior against the spec, and use code-domain with a structured issue prompt when domain problems arise. Excludes infrastructure and host implementation.
---

# Implement Application

Follow the target project's programming language, toolchain, and conventions.
Express these object-oriented design rules using the language's supported
constructs; do not assume a particular framework, SDK, or test library.

Implement only the application layer and its unit tests. Assume the domain layer
already exists. The documented specification in `spec/` is the sole source of
truth for required behavior; existing code and tests are implementation context.
Domain corrections must go through the `code-domain`.

## 1. Inspect the specification and existing domain

- Read repository instructions and locate the specification folder.
- Establish the application scope from the request and specification. Identify
  commands, queries, policies, driving contracts, driven ports, DTOs, transaction
  boundaries, event handling, authorization, and failure outcomes where specified.
  Do not generate every architectural component merely because it is mentioned
  as an available pattern.
- Inspect existing application code, domain APIs, domain tests, and project
  tooling. Map each specified workflow to the actual domain operations and
  contracts it needs. Do not assume an API is missing before inspecting it.
- Maintain a traceability table linking each in-scope requirement's specification
  path and section or rule ID to application code and unit test scenarios.
- Ask focused questions for missing or contradictory requirements and continue
  independent work. Do not invent business rules, authorization, retries,
  transaction semantics, error contracts, or integration guarantees. Do not edit
  the specification to justify implementation choices.

## 2. Handle domain problems through code-domain

When implementation, testing, review, or a user remark exposes a possible domain
problem, compare it with the specification first. Distinguish a domain defect
from incorrect application usage, a missing adapter, and a specification gap.
Correct application usage here; do not change the domain to accommodate a faulty
caller. Clarify specification gaps rather than inventing domain behavior.

For an evidenced domain defect or missing specified domain behavior, read and
use [code-domain]  again.
Supply the original application task context plus this structured extra prompt,
filled with concrete evidence:

```text
Use code-domain to address this domain issue before application work resumes.

Context and scope:
- Repository, bounded context, and affected application workflow.

Issue or remark:
- Original user remark, if any, and the observed problem.

Specification evidence:
- Specification paths and sections or rule IDs defining the required behavior.
- Expected domain behavior and its required public contract.

Observed behavior:
- Relevant domain API, implementation, and test locations.
- Minimal reproduction or failing test, expected versus actual result, and
  any executed command and output relevant to the failure.
- Separate confirmed facts from hypotheses and unverified remarks.

Application impact:
- Blocked workflow and why the existing domain cannot satisfy the specification.

Requested correction and boundaries:
- Reconcile the domain implementation and domain unit tests with the spec.
- Keep the correction focused on the evidenced issue and preserve other behavior.
- Do not add application orchestration, adapters, or undocumented business rules.

Acceptance and verification:
- Observable specification-backed scenarios that must pass.
- Run the full domain unit suite and complete code-domain's comparison
  against the specification. Report changed contracts and unresolved issues.
```

Execute that skill's workflow rather than merely suggesting it to the user.
Its verification still applies; a focused repair is not permission to skip the
domain suite or the specification comparison. Once verified, re-inspect changed
domain contracts, adapt the application, and resume this workflow. If the same
issue remains unresolved without new evidence, report the blocker and continue
independent work rather than repeatedly invoking the skill. If the skill cannot
be found, report the missing dependency; do not silently bypass it with an ad hoc
domain patch. Report an absent domain layer as an unmet prerequisite and use
`code-domain` to establish it before dependent application work proceeds.

## 3. Implement application orchestration only

- Implement specified workflows using existing domain public APIs. Keep business
  decisions, invariants, calculations, and aggregate mutation in the domain.
  Do not duplicate domain rules or bypass aggregate encapsulation.
- Implement the necessary application-owned contracts, input/output mapping,
  authorization, orchestration, queries, and policies according to the spec.
  Use injected interfaces for external dependencies; do not implement adapters,
  persistence, transport endpoints, dependency injection, or host startup.
- Follow documented ownership of repository ports and transaction completion.
  Use the specified unit-of-work contract and transaction-scoped dependencies;
  do not add a second commit when the unit of work already owns completion.
- Follow specified event collection, mapping, outbox, and dispatch semantics.
  Do not expose internal domain objects as external contracts or claim real
  transaction atomicity from mocked dependencies.
- Preserve unrelated work. Limit supporting project changes to those needed to
  build the application and run its tests. Use available application implementation
  skills when compatible with the specification.

## 4. Test and verify against the specification

- Write a complete application unit test suite for the in-scope application
  behavior, with expectations derived from the specification independently of
  current implementation output. Use compatible repository test tooling and
  the `code-tests-application` skill when available.
- Execute real application code and real domain objects. Mock injected ports and
  collaborators; return real aggregates from repository mocks. When a unit of
  work takes a callback, execute and await it in the mock so orchestration and
  domain operations actually run. Keep tests deterministic and free of external I/O.
- Cover specified successes and failures, input/result mapping, authorization,
  missing records, domain rejection, persistence requests, and transaction/event
  orchestration where applicable. Assert required and forbidden boundary effects.
  Do not repeat the entire domain suite or invent behavior to increase coverage.
- Build the relevant projects, run the complete application and domain unit
  suites, and run applicable formatting, static analysis, and architecture checks.
  Report unavailable checks and external blockers accurately.
- Re-read the specification alongside code and tests. Complete the traceability
  table and check both directions: every in-scope requirement has implementation
  and meaningful assertions, and every implemented or tested business behavior
  has specification support. Passing tests alone do not establish compliance.
- Correct application mismatches here and route domain mismatches through the
  structured `code-domain` workflow. After corrections, rerun affected
  checks and both unit suites. Do not claim completion while required behavior
  or verification remains unresolved.

Report implemented application scope, traceability, any domain issues and repair
results, verification commands and outcomes, and remaining gaps or blockers.
