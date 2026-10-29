---
name: code-bootstrap
description: Bootstrap a new project in the target project's programming language and toolchain with a Ports & Adapters architecture (Hexagonal architecture) with a domain and application layer on the inside and an infrastructure layer on the outside. The code will be driven by spec driven development. The spec and the code will both be in the same repository.
---

# Bootstrap empty project

Scaffold an empty project using the architecture supplied with this prompt.
Execute the steps below in the target repository. Do not invent business
functionality or integrations. Reuse information the user has already supplied.

## 1. Establish the inputs

Before scaffolding, collect any missing required inputs in one concise question with some clickable options.:

- **Bounded context name**: required; use it in project, package, or module names according to the selected language's conventions.
- **Company name**: optional; offer it alongside the required inputs. If omitted,
  leave out the company prefix and its separator.
- **Technology**: required; reuse the language and toolchain established by the
  user or repository. Read a matching specification in the source architecture's
  `technology-stacks/` directory when available. These documents are stack-specific
  supplements, not a list of supported languages. If no matching document exists,
  apply the language-neutral architecture rules and the project's conventions.
  Clarify missing technology choices or material gaps without defaulting to a
  different language.
- **Topology**: required; accept exactly one of these choices:

  | Choice | Architecture specification |
  | --- | --- |
  | `single-service` | `topologies/single-service.md` |
  | `modular-monolith` | `topologies/modular-monolith.md` |
  | `microservice` | `topologies/microservices.md` |



Do not ask for adapter or host requirements at this stage.

## 2. Obtain and read the architecture

1. Use the `spec-directory` skill to create a `spec/` folder.
2. Locate the source architecture directory supplied by the user or available
   in a local agent-docs checkout. Resolve relative paths against the declaring
   file. When no source is provided, use the `architecture/` directory from
   `https://github.com/mblomme/agent-docs`.
3. Place the contents of the architecture directory into the appropriate sub folder of `spec/`, preserving
   relative paths and dotfiles.
4. Read `spec/technical/llms.txt` as the entry point for all architecture
   information. Read the topology overview, the selected technology and topology
   specifications, and the core layer rules. Resolve each `depends_on` path
   relative to its declaring document and read dependencies before dependents.

## 3. Scaffold the project

Preserve existing work. Inspect the target repository before generating files,
reuse compatible configuration, and report conflicts before overwriting.
Use the selected language's compatible installed compiler, runtime, build tools,
and package manager. Respect repository version pins and lockfiles; report
unavailable required tooling rather than silently changing the language or stack.

- Create root-level `src/` and `tests/` directories.
- Follow the selected technology specification when available and the repository's language, package, module, naming, and test conventions. Apply other stack documents only to projects using those stacks.
- Follow the `topologies/<selected-topology>.md` specification for the topology instructions.
- Follow the documented layer responsibilities conventions in the `core-rules` folder.
- For the layer `application` you can already scaffold the the empty directories as mentioned in the documentation of the application layer.
- For the layers `main` and `infrastructure` create
  other folders only when they have a current purpose.
  An empty Infrastructure project is acceptable when no adapters are
  required; do not populate it with speculative folder trees. In ´main´ an empty `modules/` folder is acceptable when no setup files are required; do not populate it with speculative folder trees.
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
- The most important commands to build, run, and test the project.

Keep detailed design and architecture rules in `spec/`; link to them instead
of duplicating them in `AGENTS.md`.

## 5. Verify and report

- Restore tools and packages, build or type-check as appropriate, run the tests, and check
  formatting using the generated configuration. Report any unavailable tools,
  restore failures, or checks that could not be completed accurately.
- Confirm `AGENTS.md` links and commands resolve to the generated files.
- Confirm omitted company names leave no stray separators, project or module dependencies follow
  the architecture, and no unrequested adapter folders or template examples
  remain. Confirm the copied architecture retains its directory structure.
- Empty folders must contain a `.gitkeep` file to preserve them in source control.
- Summarize the created structure, selected inputs, verification results, and
  any unresolved issues. Include the command to run Main.

