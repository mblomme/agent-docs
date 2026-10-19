# Agent Docs

Reusable agent plugins live in [`plugins/`](plugins/). The catalogs currently include `ddd-tactical-dotnet`, a plugin with five tactical Domain-Driven Design skills for C# and .NET: `aggregate`, `repository`, `entity`, `value-object`, and `domain-service`.

## Client marketplaces

| Client | Repository catalog |
| --- | --- |
| Claude Code | [`.claude-plugin/marketplace.json`](.claude-plugin/marketplace.json) |
| Cursor | [`.cursor-plugin/marketplace.json`](.cursor-plugin/marketplace.json) |
| Codex / ChatGPT desktop | [`.agents/plugins/marketplace.json`](.agents/plugins/marketplace.json) |

All plugin source paths resolve from the repository root to `plugins/<plugin-name>/`.

### Claude Code

After these files are pushed, register the repository and install the plugin:

```sh
claude plugin marketplace add mblomme/agent-docs
claude plugin install ddd-tactical-dotnet@agent-docs
```

For a local checkout, run `claude plugin marketplace add .` from this directory instead.

### Codex

```sh
codex plugin marketplace add https://github.com/mblomme/agent-docs.git
codex plugin add ddd-tactical-dotnet@agent-docs
```

#### Repository scope with `.agents`

To make the plugin available through a target project's own catalog, copy `plugins/ddd-tactical-dotnet/` into that project's `plugins/` directory and add `.agents/plugins/marketplace.json` at its root:

```json
{
  "name": "agent-docs",
  "interface": { "displayName": "Agent Docs" },
  "plugins": [
    {
      "name": "ddd-tactical-dotnet",
      "source": {
        "source": "local",
        "path": "./plugins/ddd-tactical-dotnet"
      },
      "policy": {
        "installation": "AVAILABLE",
        "authentication": "ON_INSTALL"
      },
      "category": "Developer Tools"
    }
  ]
}
```

If the project already has a catalog, add the plugin entry to it and use that catalog's name below. This repository already includes the catalog and plugin folder.

Enable the plugin for the project in `.codex/config.toml`:

```toml
[plugins."ddd-tactical-dotnet@agent-docs"]
enabled = true
```

Commit the catalog, plugin files, and project configuration to share the setup with collaborators. Codex loads project configuration for trusted projects. Restart the ChatGPT desktop app to refresh its local catalog, then select Agent Docs in the Plugins Directory. Plugin files are still installed into the user's cache; discovery and enablement are configured in the repository.

### Cursor

Use Cursor's plugin installation flow with `https://github.com/mblomme/agent-docs`. The repository catalog lists the available plugins. Public directory publication requires a separate submission at <https://cursor.com/marketplace/publish>.

## Add a plugin

1. Create `plugins/<plugin-name>/plugin.json` with a unique kebab-case name, version, and description.
2. Put skills in `plugins/<plugin-name>/skills/<skill-name>/SKILL.md`.
3. Add `plugins/<plugin-name>/.claude-plugin/plugin.json` with the Claude-compatible metadata. Keep its name, version, description, and keywords synchronized with the portable manifest.
4. Add an entry to all three catalogs. Claude Code and Cursor use a string source such as `"./plugins/<plugin-name>"`; Codex uses `{"source": "local", "path": "./plugins/<plugin-name>"}`. Follow the existing entries for required metadata and policies.
5. Run `claude plugin validate .` and `claude plugin validate plugins/<plugin-name>`, then verify installation in each target client before publishing.

Catalogs explicitly enumerate plugins; adding a folder alone does not register it. Skill content is shared across clients.

Manifest references: [Claude Code](https://code.claude.com/docs/en/plugins/marketplace-reference), [Cursor](https://cursor.com/docs/reference/plugins), [Codex](https://developers.openai.com/plugins/build/plugins).
