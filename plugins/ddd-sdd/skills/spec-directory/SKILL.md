---
name: spec-directory
description: This skill creates a `spec/` folder at the target repository root and copies or downloads the complete architecture directory and its contents into `spec/architecture/`, preserving relative paths and dotfiles. It does not copy repository metadata such as `.git/`. If source and destination are the same directory, it uses the existing copy. It never overwrites conflicting existing files without user guidance.
---

# Spec Directory

Create a `spec/` folder based on the structure below and populate the file `spec/llms.txt` with the documented architecture information. Mention the `Bounded Context` name in the llms.txt file if available. Mention and link `spec/llms.txt` in the AGENTS.md file.

## Spec folder structure

```folder structure
- spec // Root folder for everything related to the architecture, design, tasks and goals of the project.
    - technical // Contains the technical specification of the software system
        - architecture // Contains the architecture specification
        - deployment // The deployment specification of the software system
    - product // The design and product specification of the software system
        - domain // The domain model specification
        - application // Use cases, queries, policies and other application layer specifications
        - adapters // Inbound and outbound adapters specifications
        - stake-holder-collaboration // A holistic view of the software system as the result of stakeholder collaboration(domain storytelling, event storming, interviews, etc.)
    - llms.txt // The entry point for all design information.
```

The file `spec/llms.txt` is the entry point for all design information. It contains a list of all architecture documents, their relative paths, and their dependencies.

## llms.txt contents

Based on the info in section `spec folder structure` create the contents of `spec/llms.txt`. Only describe what is documented do not invent any additional information.

## llms.txt template

Follow this template below and do not add or invent any additional information or sections.

```txt
# Specification index

Company: {Company Name} (skip if not available)
Bounded Context: {Bounded Context name}

This file is the entry point for all architecture and design information. All paths
below are relative to this file. Read applicable documents and their dependencies
before changing code.

Don't mention that the directory is empty but the goal of the directory instead.

## Specification directories

List and describe the current contents of the specification directories. Only describe what is documented in this file do not invent any additional information.
```

## Verification

- Spec folder is created at the target repository root.
- llms.txt is created and contains the documented information according to the template above.
- AGENTS.md is updated with a relative link to `spec/llms.txt`.
