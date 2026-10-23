---
name: imagine
description: Based on the provided prompt or documentation this skill imagines a domain driven design solution for the backend side. The solution is specification driven. No code is generated.
---

# Imagine

Imagine a domain driven design analysis for the backend side of the system based on the provided prompt or documentation. The solution is specification driven. No code is generated. Use the available ddd-sdd skills to create the specification.

## Steps

- The provided prompt must include the following information directly or indirectly. If it's missing, ask for it in one concise question:
  - The bounded context name (it's mostly the app name )
  - The company name
  - The technology stack
  - The topology

- Use the bootstrap skill to scaffold this empty project with the provided information.

- Turn the provided prompt and gathered information into a `product-brief` document.

- Do a careful domain driven design analysis and create in the spec folder the specification for the domain only. Limit yourself to the domain, don't imagine the application layer, the adapters or the infrastructure. Use the available ddd-skills to create the specification.

- Limit yourself to the core domain bounded context. Do not imagine any other bounded contexts. The will be created in other repositories.

- Create the domain specification documents. (Bounded context, aggregates, ...). Use the ddd-sdd skills starting with doc-*. only

- Check that the domain specification matches the created product-brief. If it doesn't, ask for clarification in one concise question.

## Rules

- Do not generate any code.
- Only domain specification is imagined. Do not imagine the application layer, the adapters or the infrastructure.
- Only the core bounded context is created in the specification
- The domain specification matches the provided prompt, documentation and project-brief. If it doesn't, ask for clarification in one concise question.
- The ddd-sdd skills are used to create the specification. 