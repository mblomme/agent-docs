---
name: spec-use-cases-common
description: Distill common command use cases, including aggregate creation, from product aggregate and bounded context specifications into _common-use-cases.md using spec-use-case and typical flows documented in the same file. Update specification indexes without generating code.
---

# Common use case specifications

Document the common application use cases implied by the product specification.
Only update the target project's specification folder; do not generate code.

## Read the specification and base skill

- Read the sibling skill `spec-use-case`.
- Locate the target product through project instructions and specification
  `llms.txt` indexes. Resolve index links relative to their declaring files.
  Follow the established product and application layout; default to
  `spec/product/application/` when no alternative is established. If several
  products or conflicting destinations remain plausible, ask which to use.
- Read the full product specification, including bounded contexts, aggregates,
  existing use cases, and an existing `_common-use-cases.md`, before selecting
  candidates. Include unindexed product documents when checking coverage.

## Distill eligible use cases

- Inventory documented aggregate creation operations and public state-changing
  methods. Derive use cases from documented intent, inputs, and operations; do
  not invent aggregate APIs, actors, authorization policies, or business behavior.
- A common use case changes exactly one aggregate root (except bulk operation on the same aggregate type) and fits one of the [typical flows](references/typical-flow.md). Include creation through a
  documented constructor or factory. Exclude queries and operations requiring
  nonstandard orchestration or changes to multiple aggregate roots.
- Compare each candidate with use cases anywhere else in the product
  specification using its goal, bounded context, aggregate, and target operation,
  rather than its name alone. A domain method description alone is not an
  already documented application use case. Do not duplicate existing use cases. Use cases in other specification files are authoritative.

## Write the combined specification

Create or update exactly one `_common-use-cases.md` in the resolved application
folder. Group entries by bounded context and aggregate, with a stable heading for
each use case. Adapt the base template to record:

- An explicit **Use Case Name** property for every entry, even when its heading
  already includes the name. Use a self-descriptive, action-oriented PascalCase
  name starting with a verb, such as `MarkAllTasksAsDone` or `CreateTask`.
  Express the user's intended action using the product's domain vocabulary;
  avoid vague names such as `Process` or `Update` without an object.
- Actors and goal.
- Input fields and their mapping to documented aggregate operation parameters,
  including the aggregate identifier for an existing aggregate.
- Target aggregate and constructor, factory, or state-changing method, with a
  link to its specification.
- Orchestration preconditions and postconditions, applicable exceptions, and
  return information: normally void, an ID, or the bare minimum needed by callers.
- A **Flow** property including the typical flows as mentioned in `typical-flow.md`.

Keep preconditions, postconditions, and exceptions at the application
orchestration level. Business rules and invariant enforcement belong exclusively
in the domain layer. Link to existing domain rules where useful; do not copy
them into use case conditions or add application-layer rule checks. Describe
authorization only when supported by the specification.

Preserve valid existing content and stable headings. Reconcile by intent and
target operation so reruns do not append duplicates. When another document now
covers an existing common entry, replace that entry with a concise pointer to
the authoritative use case rather than retaining a duplicate specification.
Do not delete unrelated documentation or change dates merely because of a rerun.

## Maintain indexes and verify

- Add or refresh links in relevant existing specification `llms.txt` files,
  following their format and preserving unrelated entries. Link the combined
  file from indexes covering its product/application scope; avoid duplicates.
  If the specification root index is absent, create a minimal `llms.txt` linking
  existing documentation and this file. Do not create extra nested indexes
  solely for this document or modify indexes outside the specification folder.
- Verify all added or changed links, including typical-flow anchors. Confirm
  that the shared typical flows are fully documented in the file, every entry
  has an action-oriented PascalCase **Use Case Name** property and a resolving
  local **Typical flow** link, and flow steps are not repeated per entry.
  Confirm each common entry changes one aggregate root (bulk is the exception), contains no business-rule
  enforcement, and is not already specified elsewhere.
- Report the output path, changed indexes, uncovered operations and missing
  details. Generate
  no separate use case files or implementation artifacts during this workflow.

## Rules
- This skill does not generate code or implementation artifacts. It only updates the specification.
- Check that all public state-changing operations on aggregates are covered by a use case, either in this file or elsewhere in the specification.
- Only one aggregate root change per use case. The only exception is a bulk operation on the same type of aggregat.
- Only use cases that change the domain are use cases; queries and multi-aggregate operations are not.
