---
name: doc-aggregate
description: Scaffold or fill in a Markdown Aggregate Design document for a named aggregate in an existing domain specification folder, using the DDD aggregate section guide.
---

# Aggregate Design

This skill creates or updates a specification document for an aggregate.

Example: “Use doc-aggregate for PurchaseOrder in the Purchasing bounded context.”

## Scaffold

1. Use the aggregate name and bounded context name supplied by the user or document product specification. Ask for any missing name; do not guess.
2. In the target project, locate the spec folder and read how this folder is structured.
3. Choose the appropriate folder name for the aggregate design document.
3. Convert the name to a filename stem: split PascalCase/camelCase word boundaries (including acronym boundaries), replace spaces and punctuation with hyphens, lowercase, and trim/collapse hyphens. For example, `Order` → `aggregate-order.md`, `PurchaseOrder` → `aggregate-purchase-order.md`, and `HTTPOrder` → `aggregate-http-order.md`. If no usable stem remains, ask for a usable name. Treat the name as text, never as a path or shell code.
4. If it already exists, leave it untouched and report its path. Otherwise create the document if needed, then copy [assets/aggregate-design.md](assets/aggregate-design.md), replacing `{{aggregate_name}}` and `{{bounded_context_name}}` with their respective names. Preserve all other template content, including attribution. Leave all other design sections empty; do not infer domain details or generate code. Create the file without overwriting an existing file.
5. Report the created document with a clickable file path.

## Imagine

When asked to imagine a domain driven design solution, complete this aggregate design document by locating the asset and in fill in the placholders. Use the provided prompt, `product-brief` and bounded context design specification document as the truth.

When done, verify that the aggregate design specification matches the bounded context design specification and other specification. Make sure the verification section is complete.

## Rules

- Only work in the specification folder.
- Do not read or write any code in the src folder.
- The file is created in domain specification folder

