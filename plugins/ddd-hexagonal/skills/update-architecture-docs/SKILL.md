---
name: update-architecture-docs
description: Replace a target project's spec/architecture folder with the canonical architecture documentation from agent-docs and update spec/llms.txt.
---

# Update Architecture Docs

Use this skill to refresh an existing project's architecture reference from this repository, `mblomme/agent-docs`. This is a full replacement: obsolete files and local edits inside `spec/architecture/` are removed. Keep product specifications and other project documentation intact.

Resolve the target repository from the user's request or workspace. Run the bundled script using its absolute path, replacing `<skill-directory>` and `<target-repository>`:

```bash
python3 <skill-directory>/scripts/update_architecture_docs.py <target-repository>
```

The script uses the architecture folder alongside this skill's source checkout when available. Otherwise it clones `https://github.com/mblomme/agent-docs.git` into a temporary directory and uses its default branch. To select a particular local checkout, add `--source-repo <agent-docs-checkout>`. Python 3 is required; fetching also requires Git and repository access. If fetching fails, report the failure or use an available checkout of this repository; do not substitute another architecture source.

The script stages the complete source directory, including dotfiles and `architecture/llms.txt`, before replacing `spec/architecture/`. It excludes `.git` metadata and refreshes a managed architecture section in `spec/llms.txt`, listing the current Markdown documents and linking the canonical architecture index. Other index content is preserved. An absent `spec/` or index is created.

After running the script, review `spec/llms.txt`. Reconcile any older architecture entries outside the managed section: remove obsolete or duplicate entries and correct stale architecture links while retaining company information, bounded context information, product links, and other unrelated content. Use `spec/architecture/llms.txt` and the copied documents as the source of descriptions and dependency rules; do not invent them or change the canonical files.

Verify that the replacement matches the source, that obsolete files are gone, and that architecture links in the specification index resolve. Report the source used, the updated paths, and any links elsewhere in the project that still refer to removed documents.
