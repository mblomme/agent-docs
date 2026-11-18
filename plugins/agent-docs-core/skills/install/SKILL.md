---
name: install
description: Installation skill for Agent Docs. Only use this skill when explicitly instructed to do so. It is not part of the normal workflow.
---

Execute the installation flow described in [MARKETPLACE.MD](./references/MARKETPLACE.MD) using the bundled Python script [install_marketplace.py](./scripts/install_marketplace.py). Replace `<skill-directory>` and `<target-workspace>` with absolute paths.

1. **Discover available and installed plugins** (clones the latest repository into a unique temp directory):

   ```bash
   python3 "<skill-directory>/scripts/install_marketplace.py" discover "<target-workspace>"
   ```

2. **Collect requested plugins** from the user following `PREREQUISITES` and `IDEMPOTENCY` in `MARKETPLACE.MD` (automatically include plugins with `"status": "installed"` when refreshing/updating; ask one simple multiple-choice confirmation at a time for each `"new"` plugin).
3. **Run the deterministic installation** (Phase 1, Antigravity, Codex, and Copilot `settings.json`):

   ```bash
   python3 "<skill-directory>/scripts/install_marketplace.py" install "<target-workspace>" --plugins <plugin-1> [<plugin-2> ...]
   ```

4. **Create or update `.github/plugin/marketplace.json`** at the repository root in GitHub Copilot format per `MARKETPLACE.MD`.
5. **Verify the complete installation** and present the summary of installed plugins, skills, MCPs, hooks, etc.:

   ```bash
   python3 "<skill-directory>/scripts/install_marketplace.py" verify "<target-workspace>" --plugins <plugin-1> [<plugin-2> ...]
   ```

- RULES
  - Never use cached versions of this repository; the script clones the latest `agent-docs` repository into a unique temp folder by default (do not pass `--source-repo` unless explicitly instructed).
  - Do not ignore script errors; resolve any reported failure before continuing.
