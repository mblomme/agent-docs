---
name: doc-aggregate
description: Scaffold or fill in a Markdown Aggregate Design document for a named aggregate in an existing hexagonal-inside/aggregates folder, using the DDD aggregate section guide.
---

# Aggregate Design

Example: “Use doc-aggregate for PurchaseOrder in the Purchasing bounded context.”

## Scaffold

1. Use the aggregate name and bounded context name supplied by the user or explicitly documented in the target project. Ask for any missing name; do not guess.
2. In the target project, locate the spec folder and read how this folder is structured.
3. Choose the appropriate folder name for the aggregate design document.
3. Convert the name to a filename stem: split PascalCase/camelCase word boundaries (including acronym boundaries), replace spaces and punctuation with hyphens, lowercase, and trim/collapse hyphens. For example, `Order` → `aggregate-order.md`, `PurchaseOrder` → `aggregate-purchase-order.md`, and `HTTPOrder` → `aggregate-http-order.md`. If no usable stem remains, ask for a usable name. Treat the name as text, never as a path or shell code.
4. If it already exists, leave it untouched and report its path. Otherwise create the document if needed, then copy [assets/aggregate-design.md](assets/aggregate-design.md), replacing `{{aggregate_name}}` and `{{bounded_context_name}}` with their respective names. Preserve all other template content, including attribution. Leave all other design sections empty; do not infer domain details or generate code. Create the file without overwriting an existing file.
5. Report the created document with a clickable file path.

## Fill in a design

When asked to complete or update a design, read [references/section-guide.md](references/section-guide.md). The scaffold workflow above remains empty by default; its existing-file preservation rule applies to scaffolding, not requested edits.

Use the user's input and existing project documentation as evidence. Preserve established content unless the requested update changes it. Ask focused questions when important information is missing; identify unconfirmed proposals and unresolved questions explicitly in the relevant section rather than presenting them as facts. Do not invent invariants, corrective policies, states, commands, or events. Keep Throughput and Size omitted, and do not generate aggregate code.
