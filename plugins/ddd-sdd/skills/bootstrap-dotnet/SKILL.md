---
name: bootstrap-dotnet
description: Bootstrap a new .NET project with a Ports & Adapters architecture (Hexagonal architecture) with a domain and application layer on the inside and an infrastructure layer on the outside. The code will be driven by spec driven development. The spec and the code will both be in the same repository.
---

# Bootstrap empty .NET project

Scaffold an empty project using the architecture supplied with this prompt.
Execute the steps below in the target repository. Do not invent business
functionality or integrations. Reuse information the user has already supplied.

## 1. Establish the inputs

Before scaffolding, collect any missing required inputs in one concise question:

- **Bounded context name**: required; use it in project and namespace names.
- **Company name**: optional; offer it alongside the required inputs. If omitted,
  leave out both the company prefix and its separating dot.
- **Technology**: required; it must match a specification available in the
  source architecture's `technology-stacks/` directory. List the available
  choices rather than inventing one. This prompt implements `dotnet.md`; if
  another stack is selected, use its matching bootstrap prompt if available,
  otherwise report that this prompt only supports .NET and request guidance.
- **Topology**: required; accept exactly one of these choices:

  | Choice | Architecture specification |
  | --- | --- |
  | `single-service` | `topologies/single-service.md` |
  | `modular-monolith` | `topologies/modular-monolith.md` |
  | `microservice` | `topologies/microservices.md` |

Do not ask for adapter or host requirements at this stage.

## 2. Obtain and read the architecture

1. Use the `spec-directory` skill to create a `spec/` folder.
2. Locate the source architecture directory. When this prompt is available as
   `architecture/bootstrap/bootstrap-dotnet.md`, its containing architecture
   directory is `../` relative to the prompt's directory. Prefer that local
   source when accessible. Resolve paths relative to the source file, not the
   target repository's working directory. 
   - When nothing is provided use:https://github.com/mblomme/agent-docs/architecture
3. Place the contents of the architecture directory into the appropriate sub folder of `spec/`, preserving
   relative paths and dotfiles.
4. Read `spec/technical/llms.txt` as the entry point for all architecture
   information. Read the topology overview, the selected technology and topology
   specifications, and the core layer rules. Resolve each `depends_on` path
   relative to its declaring document and read dependencies before dependents.

## 3. Scaffold the project

Preserve existing work. Inspect the target repository before generating files,
reuse compatible configuration, and report conflicts before overwriting.
Use a compatible installed .NET SDK and the repository's SDK pin when present;
report an unavailable required SDK rather than silently changing it.

- Create root-level `src/` and `tests/` directories.
- Follow the `technology-stacks/dotnet.md` specification for dotnet instructions.
- Follow the `topologies/<selected-topology>.md` specification for the topology instructions.
- Follow the documented layer responsibilities conventions in the `core-rules` folder.
- For the layer `Application` you can already scaffold the the empty directories as mentioned in the documentation of the application layer.
- For the layers `Main` and `Infrastructure` create
  other folders only when they have a current purpose.
  An empty Infrastructure project is acceptable when no adapters are
  required; do not populate it with speculative folder trees. In ´Main´ an empty `Modules/` folder is acceptable when no setup files are required; do not populate it with speculative folder trees.
- Don't create a README.md

## 4. Write a simple root AGENTS.md

Generate a short `AGENTS.md` at the target repository root with the actual
values and commands for the generated project. If one already exists, preserve
its instructions and request guidance for conflicting changes.

Include only:

- The bounded context, company when supplied, selected technology, and topology.
- A relative link to [`spec/llms.txt`](,
  identifying it as the entry point for all technical and product information. Instruct
  agents to read it and the applicable specifications and dependencies before
  changing code, and to report missing or conflicting guidance.
- A brief reminder that dependencies point inward, Main owns composition,
  adapters remain independent, and adapter/setup folders are added only when
  required.
- Metion to remove .gitkeep files when the folder is no longer empty.

Keep detailed design and architecture rules in `spec/`; link to them instead
of duplicating them in `AGENTS.md`.

## 5. Verify and report

- Restore tools and packages, build the solution, run the tests, and check
  formatting using the generated configuration. Report any unavailable tools,
  restore failures, or checks that could not be completed accurately.
- Confirm `AGENTS.md` links and commands resolve to the generated files.
- Confirm omitted company names leave no leading dots, project references follow
  the architecture, and no unrequested adapter folders or template examples
  remain. Confirm the copied architecture retains its directory structure.
- Empty folders must contain a `.gitkeep` file to preserve them in source control.
- Summarize the created structure, selected inputs, verification results, and
  any unresolved issues. Include the command to run Main.

