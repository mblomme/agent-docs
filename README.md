# Agent Docs

Reusable agent skills and architecture documentation for domain-driven design
(DDD), specification-driven development (SDD), and ports and adapters
(hexagonal) architecture. Use them to guide an agent from a product idea and
domain specifications through implementation and tests.

This repository is a collection of agent instructions and reference documents,
not an application or a library to add to your project.

## What's included

| Resource | Purpose |
| --- | --- |
| [`ddd-sdd`](./plugins/ddd-sdd/) | Language-agnostic skills for creating specifications, scaffolding a project, and implementing and testing domain and application code. |
| [`ddd-sdd-dotnet`](./plugins/ddd-sdd-dotnet/) | Optional agent hooks, skills and mcps suitable for .NET & C# |
| [`architecture/`](./architecture/) | Canonical architecture rules, deployment topologies, and technology-specific guidance. |

## Get started

Clone this repository to make its plugins available locally:

```sh
git clone https://github.com/mblomme/agent-docs.git
cd agent-docs
```

Install `ddd-sdd` using your agent client. The checked-in files are the
distribution; there is no build step.

### VS Code Copilot

1. Run **Chat: Install Plugin from Source** from the Command Palette.
2. Select the cloned `plugins/ddd-sdd` directory, choose the Copilot session
   target, and enable the plugin.
3. Open your target project and ask the agent to use the appropriate skill.

To install the optional .NET hook, repeat these steps with
`plugins/ddd-sdd-dotnet` and follow its [installation and trust instructions](./plugins/ddd-sdd-dotnet/README.md).
See the [VS Code plugin reference](https://code.visualstudio.com/docs/agents/reference/agent-plugin-reference)
for client details.

### Codex desktop

From the cloned repository, register its marketplace:

```sh
codex plugin marketplace add .
```

In the ChatGPT desktop app's Plugins Directory, select the **Agent Docs**
marketplace and install `ddd-sdd`. Install `ddd-sdd-dotnet` separately only if
you want its .NET checks, and review and trust its hooks first. See the
[OpenAI plugin packaging guide](https://developers.openai.com/plugins/build/plugins)
and the [.NET plugin README](./plugins/ddd-sdd-dotnet/README.md).

The repository also includes marketplace catalogs for
[Claude](./.claude-plugin/marketplace.json) and
[Cursor](./.cursor-plugin/marketplace.json), each listing `ddd-sdd`. Follow
your client's marketplace documentation to install it.

## Example workflow

In your target project, supply the agent with the product information and ask
it to use the relevant skills. For example:

1. **Bootstrap:** Ask for `code-bootstrap` to set up a project. Provide the
   bounded context name, technology, and one topology: `single-service`,
   `modular-monolith`, or `microservice`. A company name is optional.
2. **Describe the product:** Use `spec-product-brief` to create a product brief
   from your requirements.
3. **Design:** Use `spec-imagine` to develop a domain specification from the
   brief, or use individual skills such as `spec-bounded-context`,
   `spec-aggregate`, and `spec-use-case`.
4. **Implement:** Once the relevant specifications are ready, use `code-inside`
   to implement and test the domain and application layers in order.
   Infrastructure adapters and host implementation are outside its scope.

The [skill directory](./plugins/ddd-sdd/skills/) contains the complete set of
skills and their prerequisites. Skills use a target project's `spec/`
documentation as the source for design and implementation; review the
generated specifications and resolve ambiguities before relying on generated
code.

## Architecture reference

The [architecture index](./architecture/llms.txt) identifies the documents
to read and their dependencies. It covers the core domain, application,
infrastructure, and composition-root rules; single-service, modular-monolith,
and microservice topologies; and a [.NET technology supplement](./architecture/technology-stacks/dotnet.md).
The core rules are language-agnostic.

To refresh an existing project's copy, use `spec-architecture-update`. This
replaces the selected architecture directory, **removing local edits and
obsolete files inside it**; review the
[skill instructions](./plugins/ddd-sdd/skills/spec-architecture-update/SKILL.md)
before running it.
