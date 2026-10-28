---
name: imagine
description: Based on the provided prompt or documentation this skill imagines a domain driven design solution for the backend side. The solution is specification driven. No code is generated.
---

# Imagine

Imagine a domain driven design analysis for the backend side of the system based on the provided prompt or documentation. The solution is specification driven. No code is generated. Use the available ddd-sdd skills to create the specification.

## Pre-requisites

- The repository is already bootstrapped with the bootstrap skill.
- The product-brief document is available in the product specification folder.
- The repository can be built and run with the provided technology stack.

## Required information

The following information is required and blocking to create the domain specification:

- The bounded context name (it's mostly the app name )
- The company name
- The technology stack
- The topology

## Steps

- Verify the pre-requisites are met and required information is available. If not, ask for it in simple multiple choice questions.
- Use the bootstrap skill to fully scaffold the project with the provided information.
- As an expert in strategic and tactical domain driven design, do a careful analysis and create in the spec folder the specification for the domain only. Limit yourself to the domain, don't imagine the application layer, the adapters or the infrastructure. Use the available ddd-sdd skills to create the specification.
- Limit yourself to the core domain bounded context. Do not imagine any other bounded contexts. They will be created in other repositories. By seperate teams.
- Create the domain specification documents.
- Check that the domain specification matches the created product-brief. If it doesn't, ask for clarification in simple multiple choice questions.

## Rules

- Only work in the specification folder.
- Do not generate any code except.
- Use simple closed questions to ask for clarification, don't ask open questions.
- Only domain specification is imagined. Do not imagine the application layer, the adapters or the infrastructure.
- Only the core bounded context is created in the specification
- The domain specification matches the provided prompt, documentation and project-brief. If it doesn't, ask for clarification in one concise question.
- The ddd-sdd skills are used to create the specification.