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
2. Unless already answered, ask one simple multiple-choice question: "Want to
   install the CSharpier formatter?" Offer **Yes** and **No**. Keep this choice
   independent of the supplied code-style choice.
3. Scaffold using those references and the main skill's preservation, tooling,
   and folder rules. Create or reuse a single root solution compatible with the
   selected SDK and include the required source and test projects.
4. If the formatter choice is Yes, invoke
   [`install-formatter`](../install-formatter/SKILL.md). If No, skip formatter
   installation and preserve any existing formatter configuration.
5. If the supplied code-style choice is Yes, invoke
   [`install-code-style`](../install-code-style/SKILL.md). Otherwise skip it.
   This choice controls optional analyzer configuration. Do not enable hooks.
6. Perform the checks in `dotnet.md`, restore packages, build, and run tests.
   Restore local tools only when a tool manifest is configured, and check
   formatting only when a formatter is configured, using its configured version.
   A No answer introduces no formatter requirement. Stop any process
   started for verification and report failed or unavailable checks accurately.
7. Return the formatter choice, created structure, solution and entrypoint paths,
   actual restore/build/run/test commands, format-check commands when configured,
   verification results, and unresolved issues to the main workflow.
