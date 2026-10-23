---
name: doc-product-brief
description: Create or update a concise, high-level product-brief.md in the product specification folder from supplied information, and maintain relevant specification llms.txt indexes.
---

# Product Brief

Turn the user's prompt, notes, and supplied documents into a product brief that
explains why the product exists, whom it serves, and what it aims to achieve.
Produce a finished brief rather than an empty template. Do not generate code or
expand the task into detailed domain, application, or architecture design.

## Locate the specification

- Inspect the target project's instructions and specification layout. Use the
  product specification folder identified by the user or repository; the default
  is `<repository>/spec/product/`.
- If no specification layout exists and no alternative was requested, create
  `spec/product/`. If multiple plausible locations exist and context does not
  identify the target, ask one focused question before writing.
- Write exactly `product-brief.md` at the root of that product folder, normally
  `spec/product/product-brief.md`, outside its domain or application subfolders.

## Ask concise questions

Remove ambiguity and fill gaps in the supplied information by asking one concise question at a time. Avoid open-ended questions, multiple questions, or requests for additional information.

## Synthesize the brief

Read the supplied information and relevant existing product documents, including
an existing brief before updating it. Preserve established decisions and useful
content unless the new input supersedes them. Surface material contradictions
instead of silently resolving them.

Keep the entire resulting file **under 200 lines** (at most 199, including blank
lines). Use concise, high-level language and a structure suited to the product.
Always include:

- **Mission:** the product's present purpose, who it serves, and the value it offers.
- **Vision:** the future outcome or change the product aims to enable.
- **High-level goals:** the principal outcomes the product should achieve.

Include other topics when supported and useful: the problem or opportunity,
target users and stakeholders, value proposition, core capabilities, scope and
non-goals, success measures, constraints, assumptions, and open questions.
Prefer outcomes over feature inventories. Mention technical choices only when
they are supplied constraints or necessary to explain the product's direction;
leave implementation details to separate specifications.

Use the supplied information as evidence. Do not invent commitments, numerical
targets, deadlines, user research, or business facts. Derive mission, vision, and
goals from clear intent in the input; label consequential unconfirmed assumptions.
If essential purpose or audience information cannot be inferred, ask a concise
question. Capture nonblocking unknowns briefly without leaving template placeholders.

## Maintain specification indexes

Inspect `llms.txt` files within the specification folder. Update those whose
scope covers product documentation and where a brief entry helps discovery,
especially the specification root index and an existing product index.

- Add or refresh a concise description and a link to the brief, resolving the
  path relative to each index. For example, `spec/llms.txt` links to
  `./product/product-brief.md`; `spec/product/llms.txt` links to
  `./product-brief.md`.
- Follow each index's existing format, preserve unrelated entries and
  instructions, and avoid duplicate entries. Leave indexes for unrelated
  technical or domain topics untouched unless they explicitly index the brief.
- If the specification root has no `llms.txt`, create a minimal one that links
  to the brief and describes only existing documentation. Do not create extra
  nested indexes solely for this document.

## Verify and report

Count the final brief's lines and shorten it if necessary. Confirm mission,
vision, and high-level goals are present, unsupported claims are absent, and
every added or changed index link resolves from its declaring file.

Report the brief's path, its line count, the indexes changed, and any material
open questions. Link to the resulting files.
