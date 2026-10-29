---
name: install-formatter
description: Install or reuse repository-local CSharpier and configure the root .csharpierrc with printWidth 80 for a .NET C# repository.
---

# Install the .NET formatter

Resolve the target repository root from the user's request or the calling
bootstrap workflow. Inspect existing tool manifests and formatter configuration
before making changes.

1. Create or reuse `dotnet-tools.json` at the repository root and register
   CSharpier as a local tool. Preserve other tool entries and existing version
   pins; reuse a compatible configured CSharpier version instead of upgrading
   it. If CSharpier is configured in another local manifest, preserve its version
   and avoid leaving conflicting registrations. Report conflicts before
   overwriting existing configuration.
2. Create or update the root `.csharpierrc` so it specifies `printWidth: 80`.
   Preserve unrelated formatter settings. If an existing setting conflicts,
   report it before overwriting.
3. Verify that `dotnet-tools.json` exists at the repository root and registers
   CSharpier, and that the root `.csharpierrc` specifies `printWidth: 80`.
4. From the repository root, restore the local tools with
   `dotnet tool restore --tool-manifest dotnet-tools.json` and verify that the
   configured CSharpier version can run. Report unavailable tooling or restore
   failures accurately.

Report the configuration paths, installed or reused CSharpier version, and
verification results. Return the format-check command supported by that version
to the calling workflow. Do not install analyzer/code-style configuration or
enable hooks as part of this skill.
