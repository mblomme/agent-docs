---
name: spec-directory
description: This skill creates a `spec/` folder at the target repository root and copies or downloads the complete architecture directory and its contents into `spec/architecture/`, preserving relative paths and dotfiles. It does not copy repository metadata such as `.git/`. If source and destination are the same directory, it uses the existing copy. It never overwrites conflicting existing files without user guidance.
---

# Spec Directory

Create a `spec/` folder based on the structure below and populate the file `spec/llms.txt` with the documented architecture information. Mention the `Bounded Context` name in the llms.txt file if available. Mention and link `spec/llms.txt` in the AGENTS.md file.

## Spec folder structure

```folder structure
- spec // Root folder for everything related to the architecture, design, tasks and goals of the project.
    - architecture // Contains the architecture specification
    - design // The design specification of the software system
        - hexagonal-inside // The design specification for the inside of the hexagonal architecture.
        - hexagonal-outside // The design specification for the outside of the hexagonal architecture.
        - stake-holder-collaboration // A holistic view of the software system as the result of stakeholder collaboration(domain storytelling, event storming, interviews, etc.)
    - deployment // The deployment specification of the software system
    - verification // Must obey verification rules for the software system
        - acceptance
        - integration
    - llms.txt // The entry point for all design information.
```

The file `spec/llms.txt` is the entry point for all design information. It contains a list of all architecture documents, their relative paths, and their dependencies.

## llms.txt contents

Based on the info in section `spec folder structure` create the contents of `spec/llms.txt`. Only describe what is documented do not invent any additional information.

## Verification

- Spec folder is created at the target repository root.
- llms.txt is created and contains the documented architecture information.
- AGENTS.md is updated with a relative link to `spec/llms.txt`.
