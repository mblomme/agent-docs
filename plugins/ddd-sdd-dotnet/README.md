# ddd-sdd .NET plugin

A .NET quality-check hook for Agents. The
language-neutral DDD/SDD skills are in [`ddd-sdd`](../ddd-sdd/).

## Install

Review and trust the scripts and hook configuration before enabling them.

- **Codex desktop:** install `ddd-sdd-dotnet` from this repository's `.agents`
  marketplace, then trust its hooks. See the [OpenAI packaging guide](https://developers.openai.com/plugins/build/plugins).
- **VS Code Copilot:** run **Chat: Install Plugin from Source**, select
  `plugins/ddd-sdd-dotnet`, choose the Copilot session target, and enable the
  plugin. Verify that `agentStop` runs in a trusted session. See the
  [VS Code plugin reference](https://code.visualstudio.com/docs/agents/reference/agent-plugin-reference).

The checked-in plugin is the distribution; no build is needed.

## Checks

On each agent completion, the hook resolves the workspace to its Git root when
possible, then runs:

```sh
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet tool run csharpier format .
dotnet build ./SOLUTION --no-incremental -m:1 -warnaserror \
  -p:EnableNETAnalyzers=true -p:AnalysisMode=All -p:EnforceCodeStyleInBuild=true
dotnet test ./SOLUTION --no-build -m:1
```

`SOLUTION` must be the only `.sln` or `.slnx` at the repository root.
**CSharpier modifies project files.** Checks run even when nothing has changed
and fail if there is no solution or more than one, including in non-.NET
repositories.

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
```

## Supported Agents

- Codex
- VS Code Copilot
