---
name: code-bootstrap
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
2. From the target repository root, run
   `python3 <skill-directory>/scripts/scaffold.py --bounded-context <BoundedContext>`;
   include `--company <Company>` when supplied. The script creates or reuses the
   basic folders, projects, and root solution. Resolve reported conflicts without
   overwriting existing work. Then complete required project dependencies,
   topology-specific folders, and configuration using the references and the
   main skill's preservation, tooling, and folder rules.
3. Aks in a simple mutliple choice question if the CSharper formatter is wanted.
   If the choice is Yes, invoke
   [`install-formatter`](../install-formatter/SKILL.md). If No, skip formatter
   installation and preserve any existing formatter configuration.
4. Ask in a simple multiple choice question if the suplied code-style is wanted. 
   If choice is Yes, invoke
   [`install-code-style`](../install-code-style/SKILL.md). Otherwise skip it.
   This choice controls optional analyzer configuration. Do not enable hooks.
5. Ask in a simple multiple choice question if the supplied DesignTest suite is wanted. If choice is Yes, invoke
   [`install-design-tests`](../install-design-tests/SKILL.md). Otherwise skip it.
6. Perform the checks in `dotnet.md`, restore packages, build, and run tests.
   Restore local tools only when a tool manifest is configured, and check
   formatting only when a formatter is configured, using its configured version.
   A No answer introduces no formatter requirement. Stop any process
   started for verification and report failed or unavailable checks accurately.
7. Return the formatter choice, created structure, solution and entrypoint paths,
   actual restore/build/run/test commands, format-check commands when configured,
   verification results, and unresolved issues to the main workflow.
