---
name: code-bootstrap-dotnet
description: Scaffold and verify the .NET C# stack as the technology-specific step of the DDD-SDD code-bootstrap workflow.
---

# Bootstrap the .NET stack

Use this skill within `ddd-sdd:code-bootstrap`, reusing its inputs and architecture
specifications. The main skill owns input collection, specification setup, root
`AGENTS.md`, and the final report. If invoked directly, start that workflow;
when called from it, continue here without invoking it again.

1. **Read [dotnet.md](./references/dotnet.md) carefully** before scaffolding or verification.
   Apply its guardrails together with the selected topology and core architecture
   rules. Report missing inputs or conflicting guidance to the main workflow.
2. Scaffold using those references and the main skill's preservation, tooling,
   and folder rules. Create or reuse a single root solution compatible with the
   selected SDK and include the required source and test projects.
3. If the supplied code-style choice is Yes, invoke
   [`install-code-style`](../install-code-style/SKILL.md). Otherwise skip it.
   This choice controls optional analyzer configuration; the reference's
   baseline formatting requirements still apply. Do not enable hooks.
4. Perform the checks in `dotnet.md`, restore tools and packages, build, run tests,
   and check formatting using the configured tool versions. Stop any process
   started for verification and report failed or unavailable checks accurately.
5. Return the created structure, solution and entrypoint paths, actual
   restore/build/run/test/format-check commands, verification results, and
   unresolved issues to the main workflow.
