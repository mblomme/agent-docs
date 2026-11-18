---
name: refresh
description: Refresh Agent Docs. Only use this skill when explicitly instructed to do so. It is not part of the normal workflow.
---

Execute the installation flow from the `install` skill (`<install-skill-directory>/scripts/install_marketplace.py` and `MARKETPLACE.MD`), noting to the user that this updates a repository that already has installed plugins.

- Use the `discover` output to automatically include all plugins with `"status": "installed"` for updating without asking confirmation.
- Only ask confirmation (one question at a time) for plugins with `"status": "new"`.
- Run `install`, update `.github/plugin/marketplace.json`, and run `verify` before reporting the summary.
