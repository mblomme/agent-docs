import json
import os
from pathlib import Path
import shlex
import shutil
import subprocess
import sys
import tempfile
import textwrap
import unittest


ROOT = Path(__file__).resolve().parents[1]
REPOSITORY = ROOT.parent.parent
CLIENT_CONFIGS = {
    "codex": {"path": "com.openai/hooks/hooks.json", "event": "Stop"},
    "copilot": {"path": "com.github.copilot/hooks/hooks.json", "event": "agentStop"},
}

DOTNET_STUB = textwrap.dedent("""\
    import json
    import os
    from pathlib import Path
    import sys

    arguments = sys.argv[1:]
    with open(os.environ["QA_TEST_LOG"], "a") as log:
        log.write(json.dumps(arguments) + "\\n")

    if os.environ.get("QA_TEST_FORMAT") and arguments[:3] == ["tool", "run", "csharpier"]:
        Path("Program.cs").write_text("formatted")

    if os.environ.get("QA_TEST_FAIL") == " ".join(arguments[:2]):
        print('failure with "quotes" and newline')
        sys.exit(7)
""")


class PluginTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="dotnet plugin test ")
        self.base = Path(self.temporary.name)
        self.package = self.base / "plugin with spaces"
        shutil.copytree(ROOT, self.package, ignore=shutil.ignore_patterns("__pycache__", "*.pyc"))
        self.bin = self.base / "bin"
        self.bin.mkdir()
        dotnet = self.bin / "dotnet"
        dotnet.write_text(f"#!{sys.executable}\n{DOTNET_STUB}", encoding="utf-8")
        dotnet.chmod(0o755)
        self.env = os.environ.copy()
        self.env.update(
            PATH=str(self.bin) + os.pathsep + os.environ.get("PATH", ""),
            TMPDIR=str(self.base),
            QA_TEST_LOG=str(self.base / "calls.log"),
        )

    def tearDown(self):
        self.temporary.cleanup()

    def create_workspace(self, name="workspace with spaces"):
        root = self.base / name
        root.mkdir()
        (root / "Example.slnx").write_text("<Solution />", encoding="utf-8")
        (root / ".editorconfig").write_text("root = true\n", encoding="utf-8")
        (root / "dotnet-tools.json").write_text("{}", encoding="utf-8")
        (root / "Program.cs").write_text("initial", encoding="utf-8")
        return root

    def dotnet_calls(self):
        log_path = self.base / "calls.log"
        if not log_path.exists():
            return []
        return [json.loads(line) for line in log_path.read_text().splitlines()]

    def hook_command(self, client):
        config = CLIENT_CONFIGS[client]
        hook = json.loads((self.package / config["path"]).read_text())["hooks"][config["event"]][0]
        if "hooks" in hook:
            hook = hook["hooks"][0]
        command = hook["command"].replace("${PLUGIN_ROOT}", str(self.package))
        return shlex.split(command)

    def run_hook(self, client, event, *, env=None, raw=None):
        hook_input = raw if raw is not None else json.dumps(event)
        result = subprocess.run(
            self.hook_command(client),
            input=hook_input,
            capture_output=True,
            text=True,
            env=env or self.env,
            check=True,
        )
        return json.loads(result.stdout)

    @staticmethod
    def stop_event(workspace, **extra):
        event = {"cwd": str(workspace), "stop_hook_active": False}
        event.update(extra)
        return event

    def test_all_client_commands_run_and_preserve_qa_arguments(self):
        for client in CLIENT_CONFIGS:
            with self.subTest(client=client):
                workspace = self.create_workspace(client)
                self.assertEqual(self.run_hook(client, self.stop_event(workspace)), {})
        expected = [
            ["tool", "restore", "--tool-manifest", "dotnet-tools.json"],
            ["tool", "run", "csharpier", "format", "."],
            ["build", "./Example.slnx", "--no-incremental", "-m:1", "-warnaserror",
             "-p:EnableNETAnalyzers=true", "-p:AnalysisMode=All", "-p:EnforceCodeStyleInBuild=true"],
            ["test", "./Example.slnx", "--no-build", "-m:1"],
        ]
        self.assertEqual(self.dotnet_calls(), expected * len(CLIENT_CONFIGS))

    def test_each_failed_command_stops_later_checks(self):
        failing_commands = (
            "tool restore",
            "tool run",
            "build ./Example.slnx",
            "test ./Example.slnx",
        )
        for client in CLIENT_CONFIGS:
            for index, failing_command in enumerate(failing_commands):
                with self.subTest(client=client, command=failing_command):
                    workspace = self.create_workspace(f"{client}-{index}")
                    env = self.env | {"QA_TEST_FAIL": failing_command}
                    before = len(self.dotnet_calls())
                    response = self.run_hook(client, self.stop_event(workspace), env=env)
                    self.assertEqual(len(self.dotnet_calls()) - before, index + 1)
                    self.assertIn('failure with "quotes"', response["reason"])
                    self.assertEqual(response["decision"], "block")

    def test_checks_run_on_every_invocation_even_after_success(self):
        for client in CLIENT_CONFIGS:
            with self.subTest(client=client):
                workspace = self.create_workspace(client)
                event = self.stop_event(workspace)
                env = self.env | {"QA_TEST_FORMAT": "1"}
                calls_before = len(self.dotnet_calls())

                self.assertEqual(self.run_hook(client, event, env=env), {})
                self.assertEqual(self.run_hook(client, event, env=env), {})
                self.assertEqual(len(self.dotnet_calls()) - calls_before, 8)

                failure_env = env | {"QA_TEST_FAIL": "build ./Example.slnx"}
                response = self.run_hook(client, event, env=failure_env)
                self.assertEqual(response["decision"], "block")
                self.assertIn("failure", response["reason"])
                self.assertEqual(len(self.dotnet_calls()) - calls_before, 11)

    def test_solution_and_configuration_errors(self):
        workspace = self.create_workspace()
        (workspace / "Example.slnx").unlink()
        result = self.run_hook("codex", self.stop_event(workspace))
        self.assertIs(result["continue"], False)
        self.assertIn("found 0", result["systemMessage"])
        (workspace / "a.sln").write_text("")
        (workspace / "b.slnx").write_text("")
        result = self.run_hook("codex", self.stop_event(workspace))
        self.assertIn("found 2", result["systemMessage"])
        (workspace / "b.slnx").unlink()
        for name in (".editorconfig", "dotnet-tools.json"):
            path = workspace / name
            contents = path.read_text()
            path.unlink()
            result = self.run_hook("codex", self.stop_event(workspace))
            self.assertIn(name, result["reason"])
            path.write_text(contents)
        self.assertEqual(self.dotnet_calls(), [])

    def test_retry_and_invalid_input_contracts(self):
        workspace = self.create_workspace()
        env = self.env | {"QA_TEST_FAIL": "tool restore"}
        response = self.run_hook("codex", self.stop_event(workspace, stop_hook_active=True), env=env)
        self.assertIs(response["continue"], False)
        self.assertIn("failure", response["systemMessage"])
        response = self.run_hook("copilot", self.stop_event(workspace, stop_hook_active=True), env=env)
        self.assertEqual(response["decision"], "block")
        for client in CLIENT_CONFIGS:
            message_field = "systemMessage" if client == "codex" else "reason"
            for event, raw_input, message in (
                ({}, "{not valid json", "Invalid hook JSON"),
                ([], None, "Hook input must be a JSON object"),
                ({"cwd": "relative"}, None, "absolute workspace directory"),
            ):
                with self.subTest(client=client, message=message):
                    response = self.run_hook(client, event, raw=raw_input)
                    self.assertIn(message, response[message_field])
        response = self.run_hook("codex", self.stop_event(workspace, stop_hook_active="true"))
        self.assertIn("boolean", response["systemMessage"])

    def test_missing_dotnet_is_reported(self):
        workspace = self.create_workspace()
        (self.bin / "dotnet").unlink()
        (self.bin / "python3").symlink_to(sys.executable)
        (self.bin / "bash").symlink_to(shutil.which("bash"))
        env = self.env | {"PATH": str(self.bin)}
        response = self.run_hook("codex", self.stop_event(workspace), env=env)
        self.assertIn("Required command not found: dotnet", response["reason"])
        self.assertEqual(self.dotnet_calls(), [])

    def test_checks_do_not_require_hash_commands(self):
        workspace = self.create_workspace()
        (self.bin / "python3").symlink_to(sys.executable)
        (self.bin / "bash").symlink_to(shutil.which("bash"))
        for command in ("mktemp", "rm"):
            (self.bin / command).symlink_to(shutil.which(command))
        env = self.env | {"PATH": str(self.bin)}
        response = self.run_hook("codex", self.stop_event(workspace), env=env)
        self.assertEqual(response, {})
        self.assertEqual(len(self.dotnet_calls()), 4)

    def test_temporary_files_are_removed_after_success_and_failure(self):
        successful_workspace = self.create_workspace("success")
        failed_workspace = self.create_workspace("failure")
        self.assertEqual(self.run_hook("codex", self.stop_event(successful_workspace)), {})
        response = self.run_hook(
            "codex",
            self.stop_event(failed_workspace),
            env=self.env | {"QA_TEST_FAIL": "tool restore"},
        )
        self.assertEqual(response["decision"], "block")
        self.assertEqual(list(self.base.glob("ddd-sdd-dotnet-output.*")), [])

    def test_unrelated_broken_symlink_does_not_prevent_checks(self):
        workspace = self.create_workspace()
        (workspace / "broken.cs").symlink_to(workspace / "missing.cs")
        response = self.run_hook("codex", self.stop_event(workspace))
        self.assertEqual(response, {})
        self.assertEqual(len(self.dotnet_calls()), 4)

    def test_output_file_creation_errors_are_reported(self):
        workspace = self.create_workspace()
        env = self.env | {"TMPDIR": str(self.base / "missing")}
        response = self.run_hook("codex", self.stop_event(workspace), env=env)
        self.assertIn("Cannot create validation output file", response["systemMessage"])
        self.assertEqual(self.dotnet_calls(), [])

    def test_nested_git_directory_resolves_repository_root(self):
        workspace = self.create_workspace()
        subprocess.run(["git", "init", "-q", str(workspace)], check=True)
        nested = workspace / "nested"
        nested.mkdir()
        self.assertEqual(self.run_hook("copilot", self.stop_event(nested)), {})
        self.assertEqual(len(self.dotnet_calls()), 4)

    def test_plugin_layout(self):
        manifest = json.loads((self.package / "plugin.json").read_text())
        self.assertEqual(manifest["extensions"]["com.openai"]["hooks"], "./com.openai/hooks/hooks.json")
        for config in CLIENT_CONFIGS.values():
            self.assertTrue((self.package / config["path"]).is_file())
        self.assertTrue((self.package / "scripts/quality_checks.sh").is_file())
        self.assertTrue((self.package / "adapters/on_agent_stop.py").is_file())
        self.assertTrue((self.package / "skills").is_dir())
        self.assertFalse((self.package / "io.github.mblomme").exists())
        self.assertFalse((self.package / ".claude-plugin").exists())
        self.assertFalse(any(path.is_symlink() for path in self.package.rglob("*")))

    def test_marketplace_sources(self):
        expected_plugins = {
            ".claude-plugin/marketplace.json": {"ddd-sdd"},
            ".cursor-plugin/marketplace.json": {"ddd-sdd"},
            ".agents/plugins/marketplace.json": {"ddd-sdd", "ddd-sdd-dotnet"},
        }
        for path, expected_names in expected_plugins.items():
            catalog = json.loads((REPOSITORY / path).read_text())
            names = [entry["name"] for entry in catalog["plugins"]]
            self.assertEqual(set(names), expected_names, path)
            self.assertEqual(len(names), len(set(names)), f"{path}: duplicate plugin names")
            for entry in catalog["plugins"]:
                source = entry["source"]
                if isinstance(source, dict):
                    source = source["path"]
                self.assertTrue((REPOSITORY / source).is_dir(), f"{path}: {source}")
                manifest = json.loads((REPOSITORY / source / "plugin.json").read_text())
                self.assertEqual(entry["name"], manifest["name"], path)
                if path != ".agents/plugins/marketplace.json":
                    self.assertEqual(entry["description"], manifest["description"], path)


if __name__ == "__main__":
    unittest.main()
