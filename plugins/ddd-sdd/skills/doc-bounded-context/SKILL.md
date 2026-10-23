---
name: doc-bounded-context
description: Scaffold or fill in a Markdown Bounded Context Design document for a named bounded context in the folder identified by the product's llms.txt indexes, using the DDD section guide.
---

# Bounded Context Design

Example: “Use doc-bounded-context for OrderManagement.”

## Scaffold

1. Use the bounded context name supplied by the user or explicitly documented in the target project. Ask for it if missing or ambiguous; do not guess.
2. Locate the target product's specification entry point from the user's path or project instructions (normally `spec/llms.txt`). Read it and follow relevant links to product and domain `llms.txt` files to identify the folder for the named bounded context's design document. Resolve paths relative to each declaring index and use its descriptions of the product structure. Include ignored specification folders when locating indexes, but exclude dependency, build, and version-control directories. If the indexes are missing, conflicting, or do not identify a unique destination, ask the user for the intended folder before writing anything.
3. Convert the name to a filename stem: split PascalCase/camelCase word boundaries (including acronym boundaries), replace spaces and punctuation with hyphens, lowercase, and trim/collapse hyphens. For example, `OrderManagement` → `order-management`, and `HTTPGateway` → `http-gateway`. If no usable stem remains, ask for a usable name. Treat the name as text, never as a path or shell code.
4. Target `<resolved-folder>/bounded-context-<stem>.md`. If it already exists, leave it untouched and report its path. Otherwise copy [assets/bounded-context-design.md](assets/bounded-context-design.md), replacing only `{{bounded_context_name}}` with the name. Preserve all other template content, including attribution. Leave all other design sections empty; do not infer domain details or generate code. Create the file without overwriting an existing file.
5. Report the created document with a clickable file path.

## Fill in a design

When asked to complete or update a design, locate it through the product's `llms.txt` indexes as described above and read [references/section-guide.md](references/section-guide.md). The scaffold workflow above remains empty by default; its existing-file preservation rule applies to scaffolding, not requested edits.

Use the user's input and existing project documentation as evidence. Preserve established content unless the requested update changes it. Ask focused questions when important information is missing; record unconfirmed proposals under Assumptions and unresolved issues under Open Questions rather than presenting them as facts. Do not invent classifications, collaborators, messages, or business rules. Keep Verification Metrics omitted.
