---
name: doc-aggregate
description: Scaffold an empty Markdown Aggregate Design document for a named aggregate in an existing hexagonal-inside/aggregates folder. Use for aggregate design documentation scaffolding.
---

# Aggregate Design

Example: “Use doc-aggregate for PurchaseOrder in the Purchasing bounded context.”

1. Use the aggregate name and bounded context name supplied by the user or explicitly documented in the target project. Ask for any missing name; do not guess.
2. In the target project, locate existing directories named exactly `hexagonal-inside`, including ignored specification folders but excluding dependency, build, and version-control directories. If none or multiple are found, ask the user for the intended existing directory before writing anything. Do not create `hexagonal-inside`.
3. Convert the name to a filename stem: split PascalCase/camelCase word boundaries (including acronym boundaries), replace spaces and punctuation with hyphens, lowercase, and trim/collapse hyphens. For example, `Order` → `order.md`, `PurchaseOrder` → `purchase-order.md`, and `HTTPOrder` → `http-order.md`. If no usable stem remains, ask for a usable name. Treat the name as text, never as a path or shell code.
4. Target `<hexagonal-inside>/aggregates/<stem>.md`. If it already exists, leave it untouched and report its path. Otherwise create `aggregates` if needed, then copy [assets/aggregate-design.md](assets/aggregate-design.md), replacing `{{aggregate_name}}` and `{{bounded_context_name}}` with their respective names. Preserve all other template content, including attribution. Leave all other design sections empty; do not infer domain details or generate code. Create the file without overwriting an existing file.
5. Report the created document with a clickable file path.
