---
name: doc-bounded-context
description: Scaffold or fill in a Markdown Bounded Context Design document for a named bounded context in an existing hexagonal-inside folder, using the DDD Crew canvas guidance.
---

# Bounded Context Design

Example: “Use doc-bounded-context for OrderManagement.”

## Scaffold

1. Use the bounded context name supplied by the user or explicitly documented in the target project. Ask for it if missing or ambiguous; do not guess.
2. In the target project, locate existing directories named exactly `hexagonal-inside`, including ignored specification folders but excluding dependency, build, and version-control directories. If none or multiple are found, ask the user for the intended existing directory before writing anything. Do not create `hexagonal-inside`.
3. Convert the name to a filename stem: split PascalCase/camelCase word boundaries (including acronym boundaries), replace spaces and punctuation with hyphens, lowercase, and trim/collapse hyphens. For example, `OrderManagement` → `order-management`, and `HTTPGateway` → `http-gateway`. If no usable stem remains, ask for a usable name. Treat the name as text, never as a path or shell code.
4. Target `<hexagonal-inside>/bounded-context-<stem>.md`. If it already exists, leave it untouched and report its path. Otherwise copy [assets/bounded-context-design.md](assets/bounded-context-design.md), replacing only `{{bounded_context_name}}` with the name. Preserve all other template content, including attribution. Leave all other design sections empty; do not infer domain details or generate code. Create the file without overwriting an existing file.
5. Report the created document with a clickable file path.

## Fill in a design

When asked to complete or update a design, read [references/section-guide.md](references/section-guide.md). The scaffold workflow above remains empty by default; its existing-file preservation rule applies to scaffolding, not requested edits.

Use the user's input and existing project documentation as evidence. Preserve established content unless the requested update changes it. Ask focused questions when important information is missing; record unconfirmed proposals under Assumptions and unresolved issues under Open Questions rather than presenting them as facts. Do not invent classifications, collaborators, messages, or business rules. Keep Verification Metrics omitted.
