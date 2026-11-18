import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import tomllib
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[1]
REPOSITORY = ROOT.parent.parent
SCRIPT = ROOT / "skills" / "install" / "scripts" / "install_marketplace.py"

sys.path.insert(0, str(SCRIPT.parent))
import install_marketplace  # noqa: E402


def write_copilot_marketplace(git_root: Path, workspace: Path, plugin_names: list[str]) -> Path:
    plugins_entries = []
    for name in plugin_names:
        manifest = json.loads(
            (workspace / ".agents" / "plugins" / name / "plugin.json").read_text(encoding="utf-8")
        )
        rel_source = Path(os.path.relpath(workspace / ".agents" / "plugins" / name, git_root)).as_posix()
        plugins_entries.append(
            {
                "name": name,
                "source": f"./{rel_source}",
                "description": manifest["description"],
                "version": manifest["version"],
            }
        )
    catalog_path = git_root / ".github" / "plugin" / "marketplace.json"
    catalog_path.parent.mkdir(parents=True, exist_ok=True)
    catalog_path.write_text(
        json.dumps(
            {
                "name": "workspace-plugins",
                "owner": {"name": "test-owner"},
                "metadata": {"description": "Workspace plugins"},
                "plugins": plugins_entries,
            },
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    return catalog_path


class InstallMarketplaceTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="install marketplace test ")
        self.base = Path(self.temporary.name)
        self.source_repo = self.base / "source repo"
        shutil.copytree(
            REPOSITORY,
            self.source_repo,
            ignore=shutil.ignore_patterns(".git", "__pycache__", "*.pyc"),
        )
        self.workspace = self.base / "target workspace"
        self.workspace.mkdir()
        subprocess.run(["git", "init", "-q", str(self.workspace)], check=True)

    def tearDown(self):
        self.temporary.cleanup()

    def run_cli(self, *args: str, check: bool = True) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, str(SCRIPT), *args],
            capture_output=True,
            text=True,
            check=check,
        )

    def test_discover_reports_new_and_installed_plugins(self):
        initial = json.loads(
            self.run_cli("discover", str(self.workspace), "--source-repo", str(self.source_repo)).stdout
        )
        self.assertEqual(initial["marketplace_name"], "workspace-plugins")
        self.assertFalse(initial["marketplace_exists"])
        by_name = {item["name"]: item for item in initial["plugins"]}
        self.assertEqual(set(by_name), {"agent-docs-core", "ddd-sdd", "ddd-sdd-dotnet"})
        self.assertTrue(all(item["status"] == "new" for item in by_name.values()))

        self.run_cli(
            "install",
            str(self.workspace),
            "--plugins",
            "ddd-sdd",
            "--source-repo",
            str(self.source_repo),
        )

        refreshed = json.loads(
            self.run_cli("discover", str(self.workspace), "--source-repo", str(self.source_repo)).stdout
        )
        self.assertTrue(refreshed["marketplace_exists"])
        refreshed_by_name = {item["name"]: item["status"] for item in refreshed["plugins"]}
        self.assertEqual(
            refreshed_by_name,
            {
                "agent-docs-core": "new",
                "ddd-sdd": "installed",
                "ddd-sdd-dotnet": "new",
            },
        )

    def test_default_clone_uses_temporary_directory(self):
        bare_remote = self.base / "remote.git"
        work_clone = self.base / "work"
        shutil.copytree(self.source_repo, work_clone)
        subprocess.run(["git", "init", "-q", str(work_clone)], check=True)
        subprocess.run(["git", "-C", str(work_clone), "config", "user.name", "Test"], check=True)
        subprocess.run(["git", "-C", str(work_clone), "config", "user.email", "test@example.com"], check=True)
        subprocess.run(["git", "-C", str(work_clone), "add", "."], check=True)
        subprocess.run(["git", "-C", str(work_clone), "commit", "-q", "-m", "initial"], check=True)
        subprocess.run(["git", "clone", "--bare", "-q", str(work_clone), str(bare_remote)], check=True)

        with patch.object(install_marketplace, "REPOSITORY", str(bare_remote)):
            report = install_marketplace.discover(self.workspace)

        self.assertEqual(report["source"]["repository"], str(bare_remote))
        self.assertEqual(len(report["plugins"]), 3)

    def test_install_ddd_sdd_only_skips_dotnet_hooks_and_leaves_copilot_marketplace(self):
        result = self.run_cli(
            "install",
            str(self.workspace),
            "--plugins",
            "ddd-sdd",
            "--source-repo",
            str(self.source_repo),
        )
        self.assertIn("Next step: create or update .github/plugin/marketplace.json", result.stdout)
        self.assertFalse((self.workspace / ".github" / "plugin" / "marketplace.json").exists())
        self.assertFalse((self.workspace / ".codex" / "hooks.json").exists())
        self.assertFalse((self.workspace / ".agents" / "plugins" / "ddd-sdd-dotnet").exists())

        agent_copy = self.workspace / ".codex" / "agents" / "domain-build.toml"
        self.assertTrue(agent_copy.is_file())
        self.assertFalse(agent_copy.is_symlink())
        self.assertEqual(
            agent_copy.read_bytes(),
            (self.source_repo / "plugins" / "ddd-sdd" / "com.openai" / "agents" / "domain-build.toml").read_bytes(),
        )

        # verify fails before .github/plugin/marketplace.json exists
        failed_verify = self.run_cli("verify", str(self.workspace), check=False)
        self.assertEqual(failed_verify.returncode, 1)
        self.assertIn("Missing GitHub Copilot marketplace catalog", failed_verify.stderr)

        write_copilot_marketplace(self.workspace, self.workspace, ["ddd-sdd"])
        passed_verify = self.run_cli("verify", str(self.workspace))
        self.assertIn("Phase 2 (copilot marketplace): verified", passed_verify.stdout)

    def test_install_all_plugins_and_verify_full_layout(self):
        self.run_cli(
            "install",
            str(self.workspace),
            "--plugins",
            "agent-docs-core",
            "ddd-sdd",
            "ddd-sdd-dotnet",
            "--source-repo",
            str(self.source_repo),
        )

        antigravity_link = self.workspace / ".agents" / "plugins" / "ddd-sdd-dotnet" / "hooks.json"
        self.assertTrue(antigravity_link.is_symlink())
        self.assertEqual(os.readlink(antigravity_link), "com.google.antigravity/hooks.json")
        self.assertTrue(antigravity_link.resolve().is_file())

        codex_hooks_link = self.workspace / ".codex" / "hooks.json"
        self.assertTrue(codex_hooks_link.is_symlink())
        target_hooks = self.workspace / ".agents" / "plugins" / "ddd-sdd-dotnet" / "com.openai" / "hooks" / "hooks.json"
        self.assertEqual(codex_hooks_link.resolve(), target_hooks.resolve())
        hooks_text = target_hooks.read_text(encoding="utf-8")
        self.assertNotIn("${PLUGIN_ROOT}", hooks_text)
        self.assertIn(".agents/plugins/ddd-sdd-dotnet/adapters/hooks/codex/", hooks_text)

        codex_config = tomllib.loads((self.workspace / ".codex" / "config.toml").read_text(encoding="utf-8"))
        for plugin_name in ("agent-docs-core", "ddd-sdd", "ddd-sdd-dotnet"):
            self.assertIs(codex_config["plugins"][f"{plugin_name}@workspace-plugins"]["enabled"], True)

        copilot_settings = json.loads(
            (self.workspace / ".github" / "copilot" / "settings.json").read_text(encoding="utf-8")
        )
        self.assertEqual(
            copilot_settings["extraKnownMarketplaces"]["workspace-plugins"],
            {"source": {"source": "directory", "path": "."}},
        )
        for plugin_name in ("agent-docs-core", "ddd-sdd", "ddd-sdd-dotnet"):
            self.assertIs(copilot_settings["enabledPlugins"][f"{plugin_name}@workspace-plugins"], True)

        write_copilot_marketplace(
            self.workspace,
            self.workspace,
            ["agent-docs-core", "ddd-sdd", "ddd-sdd-dotnet"],
        )
        verify_result = self.run_cli(
            "verify",
            str(self.workspace),
            "--plugins",
            "agent-docs-core",
            "ddd-sdd",
            "ddd-sdd-dotnet",
        )
        self.assertIn("Phase 2 (antigravity): verified", verify_result.stdout)
        self.assertIn("Phase 2 (codex hooks): verified", verify_result.stdout)
        self.assertIn("Phase 2 (codex agents): verified 2 copied agent file(s)", verify_result.stdout)

    def test_idempotency_updates_plugins_and_preserves_unrelated_files_and_config(self):
        unrelated_agent_dir = self.workspace / ".agents" / "custom-tool"
        unrelated_agent_dir.mkdir(parents=True)
        (unrelated_agent_dir / "notes.txt").write_text("keep me", encoding="utf-8")

        custom_codex_agent = self.workspace / ".codex" / "agents" / "custom-agent.toml"
        custom_codex_agent.parent.mkdir(parents=True)
        custom_codex_agent.write_text('name = "custom"\n', encoding="utf-8")

        # Simulate an older symlink in .codex/agents that should become a regular file copy
        (self.workspace / ".codex" / "agents" / "domain-build.toml").symlink_to("broken-target")

        config_toml = self.workspace / ".codex" / "config.toml"
        config_toml.write_text(
            'model = "o3"\n\n'
            '[plugins."other@third-party"]\n'
            "enabled = false\n\n"
            '[plugins."ddd-sdd@workspace-plugins"]\n'
            "enabled = false\n",
            encoding="utf-8",
        )

        copilot_settings = self.workspace / ".github" / "copilot" / "settings.json"
        copilot_settings.parent.mkdir(parents=True)
        copilot_settings.write_text(
            json.dumps(
                {
                    "customFlag": True,
                    "extraKnownMarketplaces": {"other": {"source": {"source": "github", "repo": "org/repo"}}},
                    "enabledPlugins": {"other-plugin@other": True},
                }
            ),
            encoding="utf-8",
        )

        self.run_cli(
            "install",
            str(self.workspace),
            "--plugins",
            "ddd-sdd",
            "ddd-sdd-dotnet",
            "--source-repo",
            str(self.source_repo),
        )

        # Modify source plugin content and rerun install to confirm update overwrites cleanly
        source_skill = self.source_repo / "plugins" / "ddd-sdd" / "skills" / "bootstrap" / "SKILL.md"
        updated_text = source_skill.read_text(encoding="utf-8") + "\n<!-- updated -->\n"
        source_skill.write_text(updated_text, encoding="utf-8")

        self.run_cli(
            "install",
            str(self.workspace),
            "--plugins",
            "ddd-sdd",
            "ddd-sdd-dotnet",
            "--source-repo",
            str(self.source_repo),
        )

        installed_skill = self.workspace / ".agents" / "plugins" / "ddd-sdd" / "skills" / "bootstrap" / "SKILL.md"
        self.assertEqual(installed_skill.read_text(encoding="utf-8"), updated_text)
        self.assertEqual((unrelated_agent_dir / "notes.txt").read_text(encoding="utf-8"), "keep me")
        self.assertEqual(custom_codex_agent.read_text(encoding="utf-8"), 'name = "custom"\n')
        self.assertFalse((self.workspace / ".codex" / "agents" / "domain-build.toml").is_symlink())

        marketplace = json.loads(
            (self.workspace / ".agents" / "plugins" / "marketplace.json").read_text(encoding="utf-8")
        )
        names = [entry["name"] for entry in marketplace["plugins"]]
        self.assertEqual(names, ["ddd-sdd", "ddd-sdd-dotnet"])

        parsed_toml = tomllib.loads(config_toml.read_text(encoding="utf-8"))
        self.assertEqual(parsed_toml["model"], "o3")
        self.assertIs(parsed_toml["plugins"]["other@third-party"]["enabled"], False)
        self.assertIs(parsed_toml["plugins"]["ddd-sdd@workspace-plugins"]["enabled"], True)
        self.assertIs(parsed_toml["plugins"]["ddd-sdd-dotnet@workspace-plugins"]["enabled"], True)
        self.assertEqual(config_toml.read_text(encoding="utf-8").count('[plugins."ddd-sdd@workspace-plugins"]'), 1)

        parsed_settings = json.loads(copilot_settings.read_text(encoding="utf-8"))
        self.assertIs(parsed_settings["customFlag"], True)
        self.assertIn("other", parsed_settings["extraKnownMarketplaces"])
        self.assertIs(parsed_settings["enabledPlugins"]["other-plugin@other"], True)

    def test_nested_workspace_inside_git_repository(self):
        nested_workspace = self.workspace / "packages" / "service a"
        nested_workspace.mkdir(parents=True)

        self.run_cli(
            "install",
            str(nested_workspace),
            "--plugins",
            "ddd-sdd",
            "ddd-sdd-dotnet",
            "--source-repo",
            str(self.source_repo),
        )

        hooks_target = (
            nested_workspace / ".agents" / "plugins" / "ddd-sdd-dotnet" / "com.openai" / "hooks" / "hooks.json"
        )
        self.assertIn("packages/service a/.agents/plugins/ddd-sdd-dotnet/", hooks_target.read_text(encoding="utf-8"))
        settings = json.loads(
            (self.workspace / ".github" / "copilot" / "settings.json").read_text(encoding="utf-8")
        )
        self.assertEqual(
            settings["extraKnownMarketplaces"]["workspace-plugins"]["source"]["path"],
            "packages/service a",
        )

        write_copilot_marketplace(self.workspace, nested_workspace, ["ddd-sdd", "ddd-sdd-dotnet"])
        self.run_cli("verify", str(nested_workspace))

    def test_verify_detects_agent_symlink_and_unreplaced_plugin_root(self):
        self.run_cli(
            "install",
            str(self.workspace),
            "--plugins",
            "ddd-sdd",
            "ddd-sdd-dotnet",
            "--source-repo",
            str(self.source_repo),
        )
        write_copilot_marketplace(self.workspace, self.workspace, ["ddd-sdd", "ddd-sdd-dotnet"])

        agent_copy = self.workspace / ".codex" / "agents" / "domain-build.toml"
        agent_copy.unlink()
        agent_copy.symlink_to("../.agents/plugins/ddd-sdd/com.openai/agents/domain-build.toml")
        failed_agent = self.run_cli("verify", str(self.workspace), check=False)
        self.assertEqual(failed_agent.returncode, 1)
        self.assertIn("Expected copied regular file", failed_agent.stderr)

        agent_copy.unlink()
        shutil.copy2(
            self.workspace / ".agents" / "plugins" / "ddd-sdd" / "com.openai" / "agents" / "domain-build.toml",
            agent_copy,
        )

        hooks_file = (
            self.workspace / ".agents" / "plugins" / "ddd-sdd-dotnet" / "com.openai" / "hooks" / "hooks.json"
        )
        hooks_file.write_text(
            hooks_file.read_text(encoding="utf-8").replace(".agents/plugins/ddd-sdd-dotnet", "${PLUGIN_ROOT}"),
            encoding="utf-8",
        )
        failed_hooks = self.run_cli("verify", str(self.workspace), check=False)
        self.assertEqual(failed_hooks.returncode, 1)
        self.assertIn("Unreplaced ${PLUGIN_ROOT}", failed_hooks.stderr)


if __name__ == "__main__":
    unittest.main()
