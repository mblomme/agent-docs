---
name: code-plan
description: Create a goal-oriented, inside-out work plan and task breakdown under /spec/work/ (plan.md and tasks.md) referencing /spec without duplicating its content.
---

# Plan Work Implementation (Inside-Out)

## Goal

Provided a task by the user, classify the work type (`F` for Feature, `B` for Bug, `R` for Refactor) and produce a declarative, pointer-based plan and sub-task breakdown inside `/spec/work/<TYPE>-XXX-<Title>/` containing two files:
- `plan.md`: High-level intent, scope boundaries, inside-out layer impact triage, and declarative layer goals.
- `tasks.md`: Linked to `plan.md`, containing the **Task Traceability Table** and autonomous sub-task definitions (`<TYPE>-XXX-T-<LAYER>-<XX>`) ordered inside-out (`Domain` → `Application` → `Infrastructure` → `Main`).

Assume `/spec` is already up to date and is the sole source of truth. Do **not** modify source code, tests, or existing specifications while running this skill.

## Core Planning Principles

1. **Language & Project Agnostic:** Never include build/test/format CLI commands, language-specific syntax, or framework names in the plan or tasks. Subagents know the project conventions and how to execute their tasks.
2. **Goals Over Imperative Steps:** Describe *what target state and contracts must exist* and *what declarative conditions define completion*. Never prescribe step-by-step coding instructions, and never instruct subagents whether to write tests first or implementation first—each subagent and skill owns its execution workflow.
3. **Zero Spec Duplication (Pointer-Only Rule):** Never copy business rules, invariant prose, tables, or architectural explanations from `/spec` into `plan.md` or `tasks.md`. Reference them strictly via relative Markdown links with section anchors or rule IDs.
4. **Inside-Out Dependency Ordering:** Order layers and sub-tasks strictly from the inside out so outer layers build against verified inner contracts:
   - **1. Domain** (`<TYPE>-XXX-T-DOM-XX`)
   - **2. Application** (`<TYPE>-XXX-T-APP-XX`)
   - **3. Infrastructure** (`<TYPE>-XXX-T-INF-XX`)
   - **4. Main** (`<TYPE>-XXX-T-MAIN-XX`)
5. **Omit Unaffected Layers:** Not every plan requires changes in every layer. Explicitly mark unaffected layers as `No` in `plan.md`'s Layer Impact Summary and omit them from the layer goals and `tasks.md`.
6. **Prefixed Task Identifiers & Plan Back-Link:** Prefix every task ID with the plan identifier (`<TYPE>-XXX-T-DOM-01`, `<TYPE>-XXX-T-APP-01`, `<TYPE>-XXX-T-INF-01`, `<TYPE>-XXX-T-MAIN-01`). `tasks.md` must explicitly state and link to its parent `plan.md`.
7. **Domain Task Verification Scenarios:** Every task for the Domain layer (`<TYPE>-XXX-T-DOM-XX`) must include a **Verification** section containing a table of `Given / When / Then` scenarios covering the task's happy paths, boundaries, and invariant rejections.
8. **Completion Tracking:** Every task must have a concise Definition of Done (DOD) table of declarative, verifiable criteria. Track acceptance tests, DOD results, and verification in the Task Traceability Table; do not mark unverified work as verified.

For each task, fill the traceability table's tracking columns as follows and update them as work progresses:
- **AcceptanceTests (missing + available/pass):** Record the counts of relevant acceptance tests still missing, available, and passing. Passing tests are a subset of available tests. Use `unknown` when a count has not been established and `N/A` when acceptance tests do not apply.
- **DOD (done/fails):** Record the counts of the task's DOD criteria that are done and that have been evaluated and fail. Unevaluated criteria count as neither; initialize to `0/0`.
- **Verified (y/n):** Initialize to `n`; change to `y` only after the task's completion and relevant evidence have been verified.

## Workflow

### 1. Classify Work Type & Determine Folder (`/spec/work/<TYPE>-XXX-<Title>`)
- Determine `<TYPE>` from the user's input:
  - `F`: **Feature** — New or expanded product/technical capability (`F-XXX`).
  - `B`: **Bug** — Defect or mismatch where current behavior violates `/spec` (`B-XXX`).
  - `R`: **Refactor** — Structural or design improvement preserving `/spec`-defined behavior (`R-XXX`).
- Inspect `/spec/work/` if it exists.
- Use the `<TYPE>-XXX` identifier supplied by the user, or assign the next sequential 3-digit number (`001`, `002`, etc.) for that work item.
- Derive `<Title>` as a concise, logical name for the plan (e.g., `F-001-Recurring-Work`, `B-002-Invalid-Schedule-Date`, `R-003-Simplify-Work-Mapper`).
- Target directory: `/spec/work/<TYPE>-XXX-<Title>/`.

### 2. Discover Scope & Map Spec Pointers
- Read `/spec/llms.txt` and follow its links to the applicable product, domain, use-case, and architecture specifications.
- Inspect the existing codebase only to understand what components already exist versus what needs to be created, fixed, or refactored to satisfy `/spec`.
- If `/spec` contains contradictions or missing requirements that block planning, stop and report the gap rather than inventing requirements.

### 3. Triage Layer Impact (Inside-Out)
Evaluate which architectural layers are affected by the work:
- **Domain (`DOM`):** Aggregates, entities, value objects, domain services, domain events, or domain policies.
- **Application (`APP`):** Command use cases, queries, application policies, driving contracts, driven ports, or DTOs.
- **Infrastructure (`INF`):** Driving adapters (transports/controllers) or driven adapters (persistence repositories, mappers, external integrations).
- **Main (`MAIN`):** Composition root setup modules, dependency wiring, or lifecycle configuration.

### 4. Generate `plan.md` and `tasks.md`
1. Create `/spec/work/<TYPE>-XXX-<Title>/plan.md` following [references/plan-template.md](references/plan-template.md).
2. Create `/spec/work/<TYPE>-XXX-<Title>/tasks.md` following [references/tasks-template.md](references/tasks-template.md).
3. Verify that all relative links to `/spec/...` and between `plan.md` and `tasks.md` resolve accurately, no `/spec` knowledge is duplicated, no commands or test-ordering instructions appear, all task IDs follow `<TYPE>-XXX-T-<LAYER>-<XX>`, every task has a DOD table and tracking entries, and every Domain task includes its `Given / When / Then` verification table.

### 5. Review & Iterate
- Review the generated `plan.md` and `tasks.md` for completeness and accuracy.
- Iterate as necessary to ensure that the plan and tasks are clear.
