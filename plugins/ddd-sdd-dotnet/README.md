# ddd-sdd .NET plugin

A .NET bootstrap skill, optional formatter and code-style skills, and
quality-check hook for agents. The language-neutral DDD/SDD skills are in
[`ddd-sdd`](../ddd-sdd/).

## Install

Review and trust the scripts and hook configuration before enabling them.

- **Codex desktop:** install `ddd-sdd-dotnet` from this repository's `.agents`
  marketplace, then trust its hooks. See the [OpenAI packaging guide](https://developers.openai.com/plugins/build/plugins).
- **VS Code Copilot:** run **Chat: Install Plugin from Source**, select
  `plugins/ddd-sdd-dotnet`, choose the Copilot session target, and enable the
  plugin. Verify that `agentStop` runs in a trusted session. See the
  [VS Code plugin reference](https://code.visualstudio.com/docs/agents/reference/agent-plugin-reference).

The checked-in plugin is the distribution; no build is needed.

## Bootstrap

Install this plugin alongside `ddd-sdd` for C#/.NET bootstrapping. Start with
[`code-bootstrap`](../ddd-sdd/skills/code-bootstrap/SKILL.md); it collects the
inputs and code-style choice, sets up specifications, and loads
[`code-bootstrap-dotnet`](./skills/code-bootstrap-dotnet/SKILL.md) for .NET
scaffolding and verification. If a required technology-specific skill is
unavailable, the main workflow stops before generating files.

The .NET skill must read its bundled [.NET reference](./skills/code-bootstrap-dotnet/references/dotnet.md).
That reference defines project boundaries, naming, xUnit, and
ArchUnitNET requirements. The main workflow retains ownership of root
`AGENTS.md` and the final report. The .NET bootstrap asks "Want to install the
CSharpier formatter?" with Yes/No options, independently of the optional
analyzer/code-style choice. Enabling hooks is separate from bootstrapping.

## Formatter

Use the [`install-formatter`](./skills/install-formatter/SKILL.md) skill to install
or reuse repository-local CSharpier in the root `dotnet-tools.json` and configure
the root `.csharpierrc` with `printWidth: 80`. It preserves other tool entries,
version pins, and unrelated formatter settings, and verifies the configuration
and tool restoration. Choosing No during bootstrap skips installation and
preserves existing formatter configuration. Bootstrap checks formatting only
when a formatter is configured. The quality-check hook below still requires
CSharpier when enabled.

## Code style

Use the [`install-code-style`](./skills/install-code-style/SKILL.md) skill to configure
the target repository's root `.editorconfig` and `Directory.Build.props` from
the skill's references. It preserves unrelated settings and can be run
repeatedly. For an existing props file it retains `NoWarn` entries, adds the
required build properties, and reports conflicting values or imports rather
than overwriting them. Later project-level settings or imports and more-specific
`.editorconfig` files can still override the root configuration. The skill
requires Python 3.9+.

## Checks

The Codex `PreToolUse` hook checks the first `apply_patch` in each session. It
resolves the workspace to its Git root when possible and runs the checks below.
It records a session marker in `/tmp` before running them. If they fail, the
first edit is blocked and the agent asks for permission to continue; after the
user approves, retrying `apply_patch` passes without rerunning the checks.
Later edits in the same session also pass without checks. Other tools do not
trigger this gate.

The existing Codex `Stop` and Copilot `agentStop` hooks still run the same
checks on each agent completion:

```sh
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet tool run csharpier format .
dotnet build ./SOLUTION --no-incremental -m:1 -warnaserror \
  -p:EnableNETAnalyzers=true -p:AnalysisMode=All -p:EnforceCodeStyleInBuild=true
dotnet test ./SOLUTION --no-build -m:1
```

`SOLUTION` must be the only `.sln` or `.slnx` at the repository root.
**CSharpier modifies project files, including when run by the first-edit gate.**
Completion checks run even when nothing has changed. Both hooks fail if there
is no solution or more than one, including in non-.NET repositories.

The target needs `dotnet`, its project's SDK and dependencies, a root
`.editorconfig`, and a root `dotnet-tools.json` defining CSharpier. The hook
also requires Bash and Python 3.9+ (`python3`), and Git to resolve a
subdirectory to its repository root. On Windows, use an environment such as
WSL that provides `python3` and POSIX shell support. Restores may need network
access.

The timeout is 600 seconds. Client and environment policies can prevent checks
from running, and failures or timeouts may be fail-open. This hook does not
replace CI. Avoid enabling duplicate workspace, user, or plugin hooks, which
would run the checks twice.

On failure, Codex requests a retry; a repeated failure or invalid solution
count stops continuation. Copilot blocks completion and may request additional
turns while the problem persists.

## Verification

```sh
python3 -m unittest discover -s plugins/ddd-sdd-dotnet/tests -v
bash -n plugins/ddd-sdd-dotnet/scripts/quality_checks.sh
bash -n plugins/ddd-sdd-dotnet/scripts/pre_quality_checks.sh
```

## Supported Agents

- Codex
- VS Code Copilot
