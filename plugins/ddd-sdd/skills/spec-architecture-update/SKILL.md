---
name: spec-architecture-update
description: Replace a target project's spec/architecture folder with the canonical architecture documentation from agent-docs and update spec/llms.txt.
---

# Update Architecture Docs

## Description

Use this skill to refresh an existing project's architecture reference from this repository, `mblomme/agent-docs`. This is a full replacement: obsolete files and local edits inside `spec/architecture/` are removed. Keep product specifications and other project documentation intact.

Resolve the target repository from the user's request or workspace. Run the bundled script using its absolute path, replacing `<skill-directory>` and `<target-repository>`:

```bash
python3 <skill-directory>/scripts/update_architecture_docs.py <target-repository>
```

By default the script clones `https://github.com/mblomme/agent-docs.git` into a temporary directory and uses its default branch. To select a particular local checkout, add `--source-repo <agent-docs-checkout>`. Python 3 is required; fetching also requires Git and repository access. If fetching fails, report the failure or use an available checkout of this repository; do not substitute another architecture source.

The default destination is `spec/technical/architecture/`. Use `--architecture-dir architecture` or `--architecture-dir technical/architecture` to explicitly choose. Report the actual destination and flag any duplicate architecture directory.


The script stages the complete source directory, including dotfiles and `architecture/llms.txt`, before replacing `spec/technical/architecture/`. It excludes `.git` metadata and refreshes a managed architecture section in `spec/llms.txt`, listing the current Markdown documents and linking the canonical architecture index. Other index content is preserved. An absent `spec/` or index is created.

After running the script, review `spec/llms.txt`. Reconcile any older architecture entries outside the managed section: remove obsolete or duplicate entries and correct stale architecture links while retaining company information, bounded context information, product links, and other unrelated content. Use `spec/architecture/llms.txt` and the copied documents as the source of descriptions and dependency rules; do not invent them or change the canonical files.

Verify that the replacement matches the source, that obsolete files are gone, and that architecture links in the specification index resolve. Report the source used, the updated paths, and any links elsewhere in the project that still refer to removed documents.

Use the script’s before/after SHA-256 comparison to report added, changed, removed, and unchanged files, and whether the specification index changed. If both are unchanged, say “Already up to date”; successful replacement alone does not prove content changed. Include the source origin, branch, full commit, and architecture working-tree status (local modifications mean the commit alone does not identify copied content). Do not describe an explicit local checkout as the latest remote version without fetching evidence.

When the user names a file whose change matters, save its pre-update content outside the destination before running the script and show or summarize its before/after diff. If no before snapshot exists, report only current source equality; do not infer what the file contained before the update.
