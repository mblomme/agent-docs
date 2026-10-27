---
name: implement-domain
description: Implement the domain layer only from the repository's spec as the sole source of truth, then write a complete domain unit test suite and verify code and tested behavior against the specification. Use for specification-driven domain implementation; excludes application and infrastructure implementation.
---

# Implement Domain

Implement only the domain layer, then write its complete unit test suite.
The documented specification in `spec/` is the sole source of truth for required
behavior. Existing code, existing tests, examples, and tactical DDD conventions
are implementation context, not substitutes for documented requirements.

## 1. Inspect the specification before coding

- Locate the specification folder
- Establish the target bounded context and domain scope from the request and
  specification. Inspect existing domain projects, test projects, dependencies,
  and build configuration only after understanding the documented requirements.
- Extract the domain rules from use cases without implementing their application
  orchestration. Record terminology, ownership, creation rules, invariants,
  allowed and forbidden transitions, calculations, boundaries, errors, events,
  and reconstitution behavior wherever specified.
- Build a traceability table with a specification path and section or rule ID,
  required behavior, planned domain API, and unit test scenarios for every
  in-scope requirement. Keep the table current through verification; include
  domain-relevant architecture constraints as well as business rules.
- Identify missing, ambiguous, or contradictory requirements before implementing
  affected behavior. Ask focused questions and continue independent, fully
  specified work. If `spec/` is absent or contains no usable domain requirements,
  request the specification rather than inventing a model.

Do not invent business rules, defaults, validation limits, states, error
contracts, or events. Make routine internal implementation choices only when
they do not change specified behavior. Do not edit the specification to justify
an implementation or turn an unresolved assumption into an accepted requirement.

## 2. Implement the domain layer only

- Implement every in-scope domain requirement using the documented language and
  architecture. Preserve aggregate boundaries, invariants, encapsulation, and
  dependency direction required by the specification.
- Implement only domain types and domain-owned interfaces required by the spec.
  Do not implement application use cases, handlers, DTOs, persistence adapters,
  transport, composition, or external integrations. A use case's inclusion in
  the specification does not authorize implementing another layer.
- Keep domain behavior independent of external I/O and framework concerns as
  required by the architecture. Use explicit inputs for time, identity, or other
  nondeterministic values according to the documented design.
- Reuse compatible domain code and make targeted corrections where it disagrees
  with the specification. Preserve unrelated work. Limit supporting project or
  solution changes to those needed to build the domain and run its unit tests.
- Use available tactical implementation skills when helpful and consistent with
  the specification. They must not introduce additional business requirements.

## 3. Write the complete domain unit test suite

After implementing the domain, derive test expectations independently from the
specification and the traceability table. Never use current implementation
output as the oracle or reproduce production calculations to compute expected
results. Use explicit expected values justified by documented rules.

- Cover every in-scope domain rule and every distinct specified outcome,
  including successful operations, rejected operations, permitted and forbidden
  transitions, and meaningful combinations of interacting invariants.
- Cover documented boundaries with values below, at, and above each limit where
  meaningful. Include empty, missing, duplicate, and invalid inputs wherever
  applicable to the documented contract. Do not infer new validation rules just
  to add tests.
- Test creation, aggregate-owned entity behavior, value object validation and
  equality, domain assertions, domain service calculations, and reconstitution
  wherever applicable. Exercise owned entities through their aggregate's public
  API when required by the architecture and testing rules.
- Assert specified state changes, return values, error contracts, and domain
  events, including their payloads and count or order when specified. For
  rejected operations, verify invariant preservation and absence of partial
  state changes or success events as required by the contract.
- Test observable business behavior through public APIs. Avoid reflection,
  private-member assertions, and direct tests of tactical base classes. Mock
  only domain-owned collaborators when necessary; do not mock the behavior
  under test.
- Keep tests deterministic, isolated, and independent of external systems.
  Follow the specification's framework, organization, and Arrange/Act/Assert
  conventions; reuse compatible repository tooling. If available and compatible,
  use the domain unit testing skill for organization and testing mechanics.
- Review existing tests against the same specification. Correct obsolete
  expectations, but never weaken assertions merely to make the suite pass.

Complete means every specified domain behavior and applicable boundary has
meaningful assertions. A passing suite or a coverage percentage alone does not
establish completeness. Record unresolved requirements as gaps, not as covered.

## 4. Compare, correct, and verify

1. Build the domain and unit test projects and run the entire domain unit test
   suite, including existing domain tests. Run the applicable repository format,
   static analysis, and architecture checks. Do not implement unrelated layers
   to make verification possible; report external blockers precisely.
2. Re-read the original specification alongside the final code and tests.
   Complete the traceability table with actual implementation and test
   locations. Check in both directions: every requirement has implementation
   and tests, and every implemented or tested business rule has specification
   support. Investigate undocumented behavior, omissions, weak assertions,
   incorrect expected values, and contradictions even when all tests pass.
3. Review each test's setup, operation, and assertions against its cited rule.
   Confirm that it exercises the intended condition and would detect a violation
   of that rule. Inspect interactions between rules and rejected mutations for
   gaps that isolated happy-path tests miss.
4. Correct code that conflicts with the specification. Correct tests whose
   expectations conflict with it. If the specification itself is ambiguous or
   inconsistent, request clarification instead of choosing a business policy or
   rewriting the documentation.
5. After corrections, rerun affected checks and the full domain suite. Repeat the
   comparison until no known mismatch or uncovered in-scope requirement remains,
   or explicitly identify the unresolved blockers. Do not claim completion while
   required behavior or verification remains unresolved.

Report the implemented scope, requirement-to-code-to-test traceability, commands
run and their results, mismatches corrected, and remaining gaps or blockers.
Distinguish executed verification from inspection; never claim unrun tests pass
or promise that the implementation is error-free.
