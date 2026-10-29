---
name: install-code-style
description: Configure the repository-root .editorconfig and Directory.Build.props for .NET C# analyzer and code-style rules. Use when setting up or refreshing DDD-SDD .NET code-style configuration.
---

# Configure .NET code style

Resolve the target project repository root from the user's request or workspace.
Run the bundled script, replacing the placeholders with absolute paths:

```sh
python3 "<skill-directory>/scripts/configure_code_style.py" "<target-repository>"
```

The script requires Python 3.9 or later and an existing target directory. It
uses the bundled [editorconfig rules](./references/editorconfig) and
[build properties](./references/Directory.Build.props). Keep the bundled
scripts and references together.

The script creates `.editorconfig` if needed and appends the C# analyzer rules
in a marked, managed block. On subsequent runs it refreshes that block at the
end of the file, where it takes precedence over earlier matching sections.
Content outside the block is preserved, including existing rule definitions.
Other, more specific `.editorconfig` files may still override these rules.

The contents between `# BEGIN ddd-sdd-dotnet install-code-style` and
`# END ddd-sdd-dotnet install-code-style` are managed by this skill and will be
replaced. If the markers are missing a counterpart or are duplicated, the
script fails without changing either target file; resolve the marker conflict
before retrying. Blocks created by the former `conf-code-style` name are
migrated to the new markers; mixed or duplicate marker pairs are errors.

The script creates a missing root `Directory.Build.props` from the reference.
For an existing file, it preserves the original XML and adds a managed
property group at the end of the project. This enables analyzers and build-time
style enforcement, enables documentation generation for IDE0005, and appends
`CS1591` to the existing `NoWarn` value. The XML between the corresponding
`BEGIN` and `END` comments is managed and may be replaced on later runs.
Other project properties and `NoWarn` entries remain in place.

If existing required properties have conflicting or nonliteral values, XML or
managed markers are invalid, or the props file imports other files that cannot
be checked safely, the script reports an error without changing either target
file. Resolve the issue manually before retrying. The script supports UTF-8
props files, including a BOM, but rejects a DOCTYPE. Later imports or
project-level settings can still override the root properties. Do not ignore
script errors.

Report each target path and whether it was created, updated, or already up to
date. Running the skill twice without intervening changes must leave both files
byte-for-byte unchanged.
