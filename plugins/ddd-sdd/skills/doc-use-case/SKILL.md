---
name: doc-use-case
description: Scaffold or fill in a Markdown use case (usecase) design document.
---

# Use case design

This skill scaffolds or fills in a use case design document in markdown format.

## Use case definition

A **use case** is a step-by-step description of how a user (or external system) interacts with a system to achieve a specific goal.

It focuses on **what** the system does from the user's perspective—covering the normal flow of events as well as potential alternatives and errors—rather than **how** the system is technically built.

## Format

Read [references/section-guide.md](references/use-case-template.md) for a description of the sections in a use case design document. The skill will scaffold the sections, but it will not fill them in unless the user provides information or the project documentation contains relevant details.


## Scaffold

1. Create a use case design document in the `spec/product/application/` folder of the target project. The filename should be derived from the use case name, following the same rules as for aggregate design documents (see below). If the file already exists, leave it untouched and report its path. Otherwise, create the file according to the format described above.

2. Based on the available information in the `spec/` folder and the user's input, fill in the sections of the use case design document. Preserve any existing content unless the requested update changes it. If you don't have enough information don't guess and leave the placeholders as is.

## Rules

Verify that it's clear from the llms.txt file that the application spec folder contains also use cases.
