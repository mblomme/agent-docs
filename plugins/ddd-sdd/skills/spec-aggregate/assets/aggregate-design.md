# Aggregate Design

## 1. Bounded Context Name

{{bounded_context_name}}

## 2. Name

{{aggregate_name}}

## 3. Description

{{short description of the aggregate, its purpose, and its responsibilities}}

## 4. State Transitions

{{ a detailed table of the aggregate's states, their transitions, result, and any relevant notes or clarifications}}

## 5. Enforced Invariants

{{ a list of the aggregate's invariants, their enforcement mechanisms, and any relevant notes or clarifications}}

## 6. Corrective Policies

{{ a table of business actions that repair inconsistencies when boundaries relax invariants. Distinguish these from rules enforced immediately; numerous policies may shift complexity outside the aggregate. }}

## 7. Handled Commands

{{ a detailed table of the aggregate's commands, input parameters, business rules, invarianties, events emitted, outcome}}

## 8. Created Events

{{ a detailed table of the aggregate's events, description}}

## 9. Verification

A list of tests written in gherkin or other BDD style, including any relevant notes or clarifications. Maintain a consistent style and level of detail across all tests. Keep Throughput and Size omitted, and do not generate aggregate code.

---