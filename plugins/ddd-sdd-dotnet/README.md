# ddd-sdd .NET plugin

A .NET bootstrap skill, optional formatter and code-style skills, and
quality-check hook for agents. The language-neutral DDD/SDD skills are in
[`ddd-sdd`](../ddd-sdd/).

## Install

Review and trust the scripts and hook configuration before enabling them.

- **Codex desktop:** install `ddd-sdd-dotnet` from this repository's `.agents`
  marketplace, then trust its hooks. See the [OpenAI packaging guide](https://developers.openai.com/plugins/build/plugins).
- **Antigravity CLI:** install the plugin from this repository and enable its hooks.
  See the [Antigravity hooks reference](https://antigravity.google/docs/hooks?tab=cli).
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

The Codex `PreToolUse` hook checks the first `apply_patch` in each session.
Antigravity checks the first `write_to_file`, `replace_file_content`, or
`multi_replace_file_content` call in each conversation. It
resolves the workspace to its Git root when possible and runs the checks below.
It records a session marker in `/tmp` before running them. If they fail, the
first edit is blocked and the agent asks for permission to continue; after the
user approves, retrying `apply_patch` passes without rerunning the checks.
Later edits in the same session also pass without checks. Other tools do not
trigger this gate.

The Codex and Antigravity `Stop` and Copilot `agentStop` hooks run the same checks at agent completion when the `lib`, `src`, or `tests` folders changed since the user prompt:

```sh
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet tool run csharpier format .
dotnet build ./SOLUTION --no-incremental -m:1 -warnaserror \
  -p:EnableNETAnalyzers=true -p:AnalysisMode=All -p:EnforceCodeStyleInBuild=true
dotnet test ./SOLUTION --no-build -m:1
```

`SOLUTION` must be the only `.sln` or `.slnx` at the repository root.
At completion, `dotnet test` runs only when `lib` or `src` changed since the
user prompt; a change confined to `tests` runs the other checks. If the source
baseline or current fingerprint is unavailable, tests run as a fallback. The
first-edit gate always runs all four checks.
**CSharpier modifies project files, including when run by the first-edit gate.**
A prompt hook hashes the contents of the root `lib`, `src`, and `tests` folders.
Antigravity records this baseline at invocation 0; its hook event runs before
each model invocation, not only for user prompts. Antigravity workspaces must
contain exactly one workspace path.
Read-only turns skip completion checks, including in non-Git workspaces. Changes
outside those folders do not trigger the checks. If the baseline or current hash
is unavailable, completion checks run as a fallback. Both hooks fail if checks
run and there is no solution or more than one.

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

On a fixable failure, both clients request another turn and rerun the checks.
They run at most three checks per stop-hook continuation chain. After the third
failure, the agent is warned that the next stop will not rerun validation; the
hook then allows the stop and reports the retry limit. A new user turn or a
successful check resets the counter. If the runtime omits `stop_hook_active`,
the counter is retained until a check succeeds or the limit is reached.
Retry state is scoped to the client, workspace, and session in a private
temporary directory. A missing session identifier is reported as an error.
If retry state cannot be tracked, Copilot blocks the first stop when it can
identify it, then allows a continuation to stop with a warning instead of
blocking indefinitely.
Codex still stops continuation immediately for fatal errors such as an invalid
solution count.

## Verification

```sh
python3 -m unittest discover -s plugins/ddd-sdd-dotnet/tests -v
bash -n plugins/ddd-sdd-dotnet/scripts/quality_checks.sh
bash -n plugins/ddd-sdd-dotnet/scripts/pre_quality_checks.sh
```

## Supported Agents

- Codex
- VS Code Copilot
- Antigravity CLI
