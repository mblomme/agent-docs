# Agent Docs

Reusable agent plugins live in [`plugins/`](plugins/). The catalogs currently include `dotnet-ddd-tactical`, a skill for tactical Domain-Driven Design in C# and .NET.

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
claude plugin install dotnet-ddd-tactical@agent-docs
```

For a local checkout, run `claude plugin marketplace add .` from this directory instead.

### Codex

```sh
codex plugin marketplace add https://github.com/mblomme/agent-docs.git
codex plugin add dotnet-ddd-tactical@agent-docs
```

For local development, the repository catalog makes the plugin discoverable in supported clients. Restart the ChatGPT desktop app to refresh its local catalog, then select Agent Docs in the Plugins Directory.

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
