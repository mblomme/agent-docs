#!/usr/bin/env python3
"""Discover, install, and verify Agent Docs marketplace plugins in a workspace."""

import argparse
from contextlib import contextmanager
import json
import os
from pathlib import Path
import re
import shlex
import shutil
import subprocess
import sys
import tempfile
import tomllib
from typing import Optional


REPOSITORY = "https://github.com/mblomme/agent-docs.git"
DEFAULT_MARKETPLACE_NAME = "workspace-plugins"
DEFAULT_MARKETPLACE_DISPLAY_NAME = "Workspace Plugins"
DOTNET_PLUGIN_NAME = "ddd-sdd-dotnet"
EXCLUDED_COPY_PATTERNS = (".git", "__pycache__", "*.pyc")
TABLE_HEADER_RE = re.compile(r"^\s*\[([^\[\]]+)\]\s*(?:#.*)?$")
ENABLED_LINE_RE = re.compile(r"^(\s*enabled\s*=\s*)([^#\r\n]*?)(\s*(?:#.*)?)$")


class InstallError(Exception):
    """Raised when discovery, installation, or verification cannot proceed safely."""


@contextmanager
def source_repository(explicit: Optional[Path]):
    """Yield a local agent-docs checkout, cloning a fresh shallow copy by default."""
    if explicit is not None:
        resolved = explicit.resolve()
        if not (resolved / "plugins").is_dir():
            raise InstallError(f"Source repository does not contain a plugins directory: {resolved}")
        yield resolved
        return
    with tempfile.TemporaryDirectory(prefix="agent-docs-source-") as temporary:
        checkout = Path(temporary) / "agent-docs"
        try:
            subprocess.run(
                ["git", "clone", "--depth", "1", REPOSITORY, str(checkout)],
                check=True,
                capture_output=True,
                text=True,
            )
        except FileNotFoundError as exc:
            raise InstallError("Required command not found: git") from exc
        except subprocess.CalledProcessError as exc:
            detail = (exc.stderr or exc.stdout or "").strip()
            raise InstallError(f"Failed to clone {REPOSITORY}: {detail}") from exc
        yield checkout


def git_value(repository: Path, *arguments: str) -> str:
    """Return trimmed git command output or 'unknown' if unavailable."""
    result = subprocess.run(
        ["git", "-C", str(repository), *arguments],
        capture_output=True,
        text=True,
    )
    return result.stdout.strip() if result.returncode == 0 else "unknown"


def resolve_git_root(target_workspace: Path) -> Path:
    """Resolve the enclosing git repository root, or fall back to target_workspace."""
    result = subprocess.run(
        ["git", "-C", str(target_workspace), "rev-parse", "--show-toplevel"],
        capture_output=True,
        text=True,
    )
    if result.returncode == 0 and result.stdout.strip():
        return Path(result.stdout.strip()).resolve()
    return target_workspace.resolve()


def validate_workspace(target_workspace: Path) -> Path:
    """Validate and resolve the target workspace directory."""
    resolved = target_workspace.resolve()
    if not resolved.is_dir():
        raise InstallError(f"Target workspace directory does not exist: {target_workspace}")
    return resolved


def load_json_object(path: Path, label: str) -> dict:
    """Read and validate a JSON object file."""
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise InstallError(f"Invalid {label} at {path}: {exc}") from exc
    if not isinstance(data, dict):
        raise InstallError(f"Expected JSON object in {label} at {path}")
    return data


def write_json_object(path: Path, data: dict) -> None:
    """Write a JSON object formatted consistently with a trailing newline."""
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def load_source_plugins(repository: Path) -> dict[str, dict]:
    """Load metadata for all valid plugins under repository/plugins."""
    plugins_dir = repository / "plugins"
    if not plugins_dir.is_dir():
        raise InstallError(f"Plugins directory missing in source repository: {plugins_dir}")
    catalog: dict[str, dict] = {}
    for entry in sorted(plugins_dir.iterdir(), key=lambda item: item.name):
        manifest_path = entry / "plugin.json"
        if not entry.is_dir() or not manifest_path.is_file():
            continue
        manifest = load_json_object(manifest_path, f"plugin manifest ({entry.name})")
        name = manifest.get("name")
        if not isinstance(name, str) or not name or name != entry.name:
            raise InstallError(f"Plugin manifest name mismatch in {manifest_path}")
        catalog[name] = {
            "name": name,
            "version": manifest.get("version", ""),
            "description": manifest.get("description", ""),
            "path": entry,
        }
    if not catalog:
        raise InstallError(f"No plugins found in {plugins_dir}")
    return catalog


def inspect_marketplace(target_workspace: Path) -> tuple[Optional[dict], set[str]]:
    """Inspect existing .agents/plugins directory and marketplace.json."""
    plugins_dir = target_workspace / ".agents" / "plugins"
    marketplace_path = plugins_dir / "marketplace.json"
    marketplace_data: Optional[dict] = None
    installed: set[str] = set()
    if plugins_dir.is_dir():
        for entry in plugins_dir.iterdir():
            if entry.is_dir() and not entry.name.startswith("."):
                installed.add(entry.name)
    if marketplace_path.exists():
        marketplace_data = load_json_object(marketplace_path, "marketplace.json")
        entries = marketplace_data.get("plugins", [])
        if not isinstance(entries, list):
            raise InstallError(f"Expected 'plugins' array in {marketplace_path}")
        for entry in entries:
            if isinstance(entry, dict) and isinstance(entry.get("name"), str):
                installed.add(entry["name"])
    return marketplace_data, installed


def discover(target_workspace: Path, source_repo: Optional[Path] = None) -> dict:
    """Inspect available source plugins and target workspace installation status."""
    workspace = validate_workspace(target_workspace)
    git_root = resolve_git_root(workspace)
    marketplace_data, installed = inspect_marketplace(workspace)
    marketplace_name = (
        marketplace_data.get("name")
        if marketplace_data and isinstance(marketplace_data.get("name"), str) and marketplace_data.get("name")
        else DEFAULT_MARKETPLACE_NAME
    )
    with source_repository(source_repo) as repository:
        catalog = load_source_plugins(repository)
        source_info = {
            "repository": str(source_repo.resolve()) if source_repo is not None else REPOSITORY,
            "origin": git_value(repository, "remote", "get-url", "origin"),
            "branch": git_value(repository, "branch", "--show-current") or "(detached HEAD)",
            "commit": git_value(repository, "rev-parse", "HEAD"),
        }
        plugins = [
            {
                "name": item["name"],
                "version": item["version"],
                "description": item["description"],
                "status": "installed" if item["name"] in installed else "new",
            }
            for item in catalog.values()
        ]
    return {
        "source": source_info,
        "target_workspace": str(workspace),
        "git_root": str(git_root),
        "marketplace_exists": marketplace_data is not None,
        "marketplace_name": marketplace_name,
        "plugins": plugins,
    }


def deduplicate_names(names: list[str]) -> list[str]:
    """Return unique plugin names in their original order."""
    seen: set[str] = set()
    ordered: list[str] = []
    for name in names:
        if "/" in name or "\\" in name or name in (".", ".."):
            raise InstallError(f"Invalid plugin name: {name}")
        if name not in seen:
            seen.add(name)
            ordered.append(name)
    if not ordered:
        raise InstallError("At least one plugin name must be provided")
    return ordered


def install_plugin_directory(source_dir: Path, destination_dir: Path) -> None:
    """Atomically copy source_dir to destination_dir while preserving sibling entries."""
    if destination_dir.is_symlink() or (destination_dir.exists() and not destination_dir.is_dir()):
        raise InstallError(f"Plugin destination is not a directory: {destination_dir}")
    resolved_source = source_dir.resolve()
    resolved_dest = destination_dir.resolve()
    if (resolved_source == resolved_dest
            or resolved_source in resolved_dest.parents
            or resolved_dest in resolved_source.parents):
        raise InstallError("Source and destination plugin directories must not overlap")

    with tempfile.TemporaryDirectory(prefix=".plugin-install-", dir=destination_dir.parent) as temporary:
        staging = Path(temporary)
        staged = staging / destination_dir.name
        backup = staging / "previous"
        shutil.copytree(source_dir, staged, ignore=shutil.ignore_patterns(*EXCLUDED_COPY_PATTERNS))
        had_previous = destination_dir.exists()
        if had_previous:
            destination_dir.rename(backup)
        try:
            staged.rename(destination_dir)
        except OSError:
            if destination_dir.exists():
                shutil.rmtree(destination_dir)
            if had_previous:
                backup.rename(destination_dir)
            raise


def update_local_marketplace(workspace: Path, selected_plugins: list[str]) -> str:
    """Create or update .agents/plugins/marketplace.json and return marketplace name."""
    plugins_dir = workspace / ".agents" / "plugins"
    marketplace_path = plugins_dir / "marketplace.json"
    if marketplace_path.exists():
        data = load_json_object(marketplace_path, "marketplace.json")
        if "plugins" not in data:
            data["plugins"] = []
        if not isinstance(data["plugins"], list):
            raise InstallError(f"Expected 'plugins' array in {marketplace_path}")
    else:
        data = {
            "name": DEFAULT_MARKETPLACE_NAME,
            "interface": {"displayName": DEFAULT_MARKETPLACE_DISPLAY_NAME},
            "plugins": [],
        }

    marketplace_name = data.get("name")
    if not isinstance(marketplace_name, str) or not marketplace_name:
        marketplace_name = DEFAULT_MARKETPLACE_NAME
        data["name"] = marketplace_name

    existing_by_name: dict[str, int] = {}
    for index, entry in enumerate(data["plugins"]):
        if not isinstance(entry, dict) or not isinstance(entry.get("name"), str):
            raise InstallError(f"Malformed plugin entry in {marketplace_path}")
        if entry["name"] in existing_by_name:
            raise InstallError(f"Duplicate plugin '{entry['name']}' in {marketplace_path}")
        existing_by_name[entry["name"]] = index

    for name in selected_plugins:
        entry = {
            "name": name,
            "source": {
                "source": "local",
                "path": f"./.agents/plugins/{name}",
            },
            "policy": {
                "installation": "INSTALLED_BY_DEFAULT",
                "authentication": "ON_INSTALL",
            },
            "category": "Productivity",
        }
        if name in existing_by_name:
            data["plugins"][existing_by_name[name]] = entry
        else:
            existing_by_name[name] = len(data["plugins"])
            data["plugins"].append(entry)

    write_json_object(marketplace_path, data)
    return marketplace_name


def replace_symlink(link_path: Path, target: str) -> None:
    """Create or replace a symlink pointing to target."""
    if link_path.exists() and not link_path.is_symlink() and link_path.is_dir():
        raise InstallError(f"Cannot replace directory with symlink: {link_path}")
    link_path.parent.mkdir(parents=True, exist_ok=True)
    link_path.unlink(missing_ok=True)
    link_path.symlink_to(target)


def configure_antigravity(workspace: Path, selected_plugins: list[str]) -> None:
    """Copy Antigravity subagents into <plugin-root>/agents and symlink ddd-sdd-dotnet hooks.json."""
    copy_antigravity_agents(workspace, selected_plugins)
    if DOTNET_PLUGIN_NAME not in selected_plugins:
        return
    plugin_root = workspace / ".agents" / "plugins" / DOTNET_PLUGIN_NAME
    source_hooks = plugin_root / "com.google.antigravity" / "hooks.json"
    if not source_hooks.is_file():
        raise InstallError(f"Missing Antigravity hooks file: {source_hooks}")
    replace_symlink(plugin_root / "hooks.json", "com.google.antigravity/hooks.json")


def rewrite_codex_hooks_plugin_root(hooks_path: Path, git_root: Path, plugin_root: Path) -> None:
    """Replace ${PLUGIN_ROOT} in Codex hooks.json and verify resolved hook scripts."""
    relative_root = Path(os.path.relpath(plugin_root, git_root)).as_posix()
    raw_text = hooks_path.read_text(encoding="utf-8")
    updated_text = raw_text.replace("${PLUGIN_ROOT}", relative_root)
    try:
        parsed = json.loads(updated_text)
    except json.JSONDecodeError as exc:
        raise InstallError(f"Invalid JSON after updating {hooks_path}: {exc}") from exc
    if not isinstance(parsed, dict):
        raise InstallError(f"Expected JSON object in {hooks_path}")
    validate_codex_hook_commands(parsed, git_root, hooks_path)
    hooks_path.write_text(updated_text, encoding="utf-8")


def iter_codex_hook_commands(hooks_data: dict) -> list[str]:
    """Extract command strings from a Codex hooks.json structure."""
    commands: list[str] = []
    hooks_section = hooks_data.get("hooks", {})
    if not isinstance(hooks_section, dict):
        return commands
    for matchers in hooks_section.values():
        if not isinstance(matchers, list):
            continue
        for matcher in matchers:
            if not isinstance(matcher, dict):
                continue
            for hook in matcher.get("hooks", []):
                if isinstance(hook, dict) and isinstance(hook.get("command"), str):
                    commands.append(hook["command"])
    return commands


def validate_codex_hook_commands(hooks_data: dict, git_root: Path, hooks_path: Path) -> None:
    """Ensure every script path referenced by Codex hooks resolves from git_root."""
    commands = iter_codex_hook_commands(hooks_data)
    if not commands:
        raise InstallError(f"No hook commands found in {hooks_path}")
    for command in commands:
        if "${PLUGIN_ROOT}" in command:
            raise InstallError(f"Unreplaced ${{PLUGIN_ROOT}} in {hooks_path}")
        tokens = shlex.split(command)
        script_tokens = [token for token in tokens if token.endswith(".py")]
        if not script_tokens:
            raise InstallError(f"Expected Python script path in hook command '{command}' in {hooks_path}")
        for script_token in script_tokens:
            candidate = (git_root / script_token).resolve()
            if not candidate.is_file():
                raise InstallError(
                    f"Hook command path '{script_token}' in {hooks_path} does not resolve under {git_root}"
                )


def copy_client_agents(
    workspace: Path,
    selected_plugins: list[str],
    client_ext: str,
    dest_dir: Path,
    client_label: str,
) -> list[Path]:
    """Copy subagent files from each selected plugin's <client_ext>/agents into dest_dir."""
    dest_dir.mkdir(parents=True, exist_ok=True)
    copied: list[Path] = []
    seen_filenames: dict[str, str] = {}
    for plugin_name in selected_plugins:
        agents_src = workspace / ".agents" / "plugins" / plugin_name / client_ext / "agents"
        if not agents_src.is_dir():
            continue
        for agent_file in sorted(agents_src.iterdir(), key=lambda item: item.name):
            if not agent_file.is_file():
                continue
            if agent_file.name in seen_filenames:
                raise InstallError(
                    f"Duplicate {client_label} agent filename '{agent_file.name}' across "
                    f"{seen_filenames[agent_file.name]} and {plugin_name}"
                )
            seen_filenames[agent_file.name] = plugin_name
            dest = dest_dir / agent_file.name
            if dest.is_dir() and not dest.is_symlink():
                raise InstallError(f"Cannot overwrite directory with agent file: {dest}")
            if dest.is_symlink():
                dest.unlink()
            shutil.copy2(agent_file, dest)
            copied.append(dest)
    return copied


def copy_antigravity_agents(workspace: Path, selected_plugins: list[str]) -> list[Path]:
    """Copy subagent files from each selected plugin's com.google.antigravity/agents into <plugin-root>/agents/."""
    copied: list[Path] = []
    for plugin_name in selected_plugins:
        plugin_root = workspace / ".agents" / "plugins" / plugin_name
        if not (plugin_root / "com.google.antigravity" / "agents").is_dir():
            continue
        copied.extend(
            copy_client_agents(
                workspace,
                [plugin_name],
                "com.google.antigravity",
                plugin_root / "agents",
                "Antigravity",
            )
        )
    return copied


def copy_codex_agents(workspace: Path, git_root: Path, selected_plugins: list[str]) -> list[Path]:
    """Copy subagent files from each selected plugin's com.openai/agents into .codex/agents/."""
    return copy_client_agents(
        workspace,
        selected_plugins,
        "com.openai",
        git_root / ".codex" / "agents",
        "Codex",
    )


def parse_toml_table_name(header: str) -> Optional[tuple[str, ...]]:
    """Parse a simple dotted TOML table header into path segments."""
    stripped = header.strip()
    if not stripped:
        return None
    try:
        parsed = tomllib.loads(f"[{stripped}]\n")
    except tomllib.TOMLDecodeError:
        return None
    segments: list[str] = []
    current = parsed
    while isinstance(current, dict) and len(current) == 1:
        key, value = next(iter(current.items()))
        segments.append(key)
        current = value
    return tuple(segments) if isinstance(current, dict) else None


def upsert_codex_config_toml(existing_text: str, plugin_keys: list[str]) -> str:
    """Update or add [plugins."<name>@<marketplace>"] enabled = true sections in TOML."""
    if existing_text.strip():
        try:
            tomllib.loads(existing_text)
        except tomllib.TOMLDecodeError as exc:
            raise InstallError(f"Existing .codex/config.toml is invalid TOML: {exc}") from exc

    lines = existing_text.splitlines(keepends=True)
    remaining = set(plugin_keys)
    output: list[str] = []
    index = 0

    while index < len(lines):
        line = lines[index]
        match = TABLE_HEADER_RE.match(line)
        table_path = parse_toml_table_name(match.group(1)) if match else None
        if table_path and len(table_path) == 2 and table_path[0] == "plugins" and table_path[1] in remaining:
            plugin_key = table_path[1]
            remaining.remove(plugin_key)
            output.append(line)
            index += 1
            section_lines: list[str] = []
            while index < len(lines) and not TABLE_HEADER_RE.match(lines[index]):
                section_lines.append(lines[index])
                index += 1
            replaced_enabled = False
            for sec_line in section_lines:
                enabled_match = ENABLED_LINE_RE.match(sec_line.rstrip("\r\n"))
                if enabled_match and not replaced_enabled:
                    newline = "\r\n" if sec_line.endswith("\r\n") else ("\n" if sec_line.endswith("\n") else "")
                    output.append(f"{enabled_match.group(1)}true{enabled_match.group(3)}{newline}")
                    replaced_enabled = True
                elif enabled_match:
                    continue
                else:
                    output.append(sec_line)
            if not replaced_enabled:
                if output and not output[-1].endswith("\n"):
                    output[-1] = output[-1] + "\n"
                output.append("enabled = true\n")
            continue

        output.append(line)
        index += 1

    result = "".join(output)
    for plugin_key in plugin_keys:
        if plugin_key not in remaining:
            continue
        if result and not result.endswith("\n"):
            result += "\n"
        if result and not result.endswith("\n\n"):
            result += "\n"
        escaped_key = plugin_key.replace("\\", "\\\\").replace('"', '\\"')
        result += f'[plugins."{escaped_key}"]\nenabled = true\n'

    try:
        parsed = tomllib.loads(result)
    except tomllib.TOMLDecodeError as exc:
        raise InstallError(f"Generated .codex/config.toml is invalid TOML: {exc}") from exc
    plugins_table = parsed.get("plugins", {})
    for plugin_key in plugin_keys:
        if not isinstance(plugins_table.get(plugin_key), dict) or plugins_table[plugin_key].get("enabled") is not True:
            raise InstallError(f"Failed to enable '{plugin_key}' in .codex/config.toml")
    return result


def configure_codex(
    workspace: Path,
    git_root: Path,
    selected_plugins: list[str],
    marketplace_name: str,
) -> None:
    """Configure Codex hooks, subagent copies, and config.toml under git_root/.codex."""
    codex_dir = git_root / ".codex"
    codex_dir.mkdir(parents=True, exist_ok=True)

    if DOTNET_PLUGIN_NAME in selected_plugins:
        plugin_root = workspace / ".agents" / "plugins" / DOTNET_PLUGIN_NAME
        hooks_target = plugin_root / "com.openai" / "hooks" / "hooks.json"
        if not hooks_target.is_file():
            raise InstallError(f"Missing Codex hooks file: {hooks_target}")
        rewrite_codex_hooks_plugin_root(hooks_target, git_root, plugin_root)
        symlink_target = Path(os.path.relpath(hooks_target, codex_dir)).as_posix()
        replace_symlink(codex_dir / "hooks.json", symlink_target)

    copy_codex_agents(workspace, git_root, selected_plugins)

    config_path = codex_dir / "config.toml"
    existing_toml = config_path.read_text(encoding="utf-8") if config_path.exists() else ""
    plugin_keys = [f"{name}@{marketplace_name}" for name in selected_plugins]
    updated_toml = upsert_codex_config_toml(existing_toml, plugin_keys)
    config_path.write_text(updated_toml, encoding="utf-8")


def configure_copilot_settings(
    workspace: Path,
    git_root: Path,
    selected_plugins: list[str],
    marketplace_name: str,
) -> None:
    """Create or update .github/copilot/settings.json without touching marketplace.json."""
    settings_path = git_root / ".github" / "copilot" / "settings.json"
    if settings_path.exists():
        settings = load_json_object(settings_path, "Copilot settings.json")
    else:
        settings = {}

    extra_marketplaces = settings.setdefault("extraKnownMarketplaces", {})
    if not isinstance(extra_marketplaces, dict):
        raise InstallError(f"Expected 'extraKnownMarketplaces' object in {settings_path}")
    rel_workspace = Path(os.path.relpath(workspace, git_root)).as_posix()
    extra_marketplaces[marketplace_name] = {
        "source": {
            "source": "directory",
            "path": rel_workspace,
        }
    }

    enabled_plugins = settings.setdefault("enabledPlugins", {})
    if not isinstance(enabled_plugins, dict):
        raise InstallError(f"Expected 'enabledPlugins' object in {settings_path}")
    for name in selected_plugins:
        enabled_plugins[f"{name}@{marketplace_name}"] = True

    write_json_object(settings_path, settings)


def install(
    target_workspace: Path,
    requested_plugins: list[str],
    source_repo: Optional[Path] = None,
) -> list[str]:
    """Execute Phase 1 and deterministic Phase 2 installation steps."""
    workspace = validate_workspace(target_workspace)
    git_root = resolve_git_root(workspace)
    selected = deduplicate_names(requested_plugins)

    with source_repository(source_repo) as repository:
        catalog = load_source_plugins(repository)
        unknown = [name for name in selected if name not in catalog]
        if unknown:
            available = ", ".join(catalog.keys())
            raise InstallError(f"Unknown plugin(s): {', '.join(unknown)}. Available: {available}")

        plugins_dir = workspace / ".agents" / "plugins"
        plugins_dir.mkdir(parents=True, exist_ok=True)
        for name in selected:
            install_plugin_directory(catalog[name]["path"], plugins_dir / name)

    marketplace_name = update_local_marketplace(workspace, selected)
    configure_antigravity(workspace, selected)
    configure_codex(workspace, git_root, selected, marketplace_name)
    configure_copilot_settings(workspace, git_root, selected, marketplace_name)

    checks = verify_installation(
        workspace,
        selected,
        include_copilot_marketplace=False,
    )
    return checks


def verify_client_agents(
    plugins_dir: Path,
    plugins_to_check: list[str],
    client_ext: str,
    dest_dir: Path,
    client_label: str,
) -> list[Path]:
    """Verify every subagent in <client_ext>/agents has a matching regular file copy in dest_dir."""
    expected_agent_files: list[Path] = []
    for name in plugins_to_check:
        agents_src = plugins_dir / name / client_ext / "agents"
        if agents_src.is_dir():
            expected_agent_files.extend(item for item in sorted(agents_src.iterdir()) if item.is_file())
    if expected_agent_files and not dest_dir.is_dir():
        raise InstallError(f"Missing {client_label} agents directory: {dest_dir}")
    for src_agent in expected_agent_files:
        copied_agent = dest_dir / src_agent.name
        if copied_agent.is_symlink() or not copied_agent.is_file():
            raise InstallError(f"Expected copied regular file at {copied_agent}")
        if copied_agent.read_bytes() != src_agent.read_bytes():
            raise InstallError(f"{client_label} agent copy differs from source: {copied_agent}")
    return expected_agent_files


def verify_copilot_marketplace(
    workspace: Path,
    git_root: Path,
    expected_plugins: list[str],
) -> None:
    """Verify .github/plugin/marketplace.json compatibility and plugin registration."""
    catalog_path = git_root / ".github" / "plugin" / "marketplace.json"
    if not catalog_path.is_file():
        raise InstallError(f"Missing GitHub Copilot marketplace catalog: {catalog_path}")
    catalog = load_json_object(catalog_path, "GitHub Copilot marketplace.json")

    name = catalog.get("name")
    if not isinstance(name, str) or not name.strip():
        raise InstallError(f"Missing marketplace 'name' in {catalog_path}")
    owner = catalog.get("owner")
    if not isinstance(owner, dict) or not isinstance(owner.get("name"), str) or not owner["name"].strip():
        raise InstallError(f"Missing 'owner.name' in {catalog_path}")

    entries = catalog.get("plugins")
    if not isinstance(entries, list):
        raise InstallError(f"Expected 'plugins' array in {catalog_path}")

    seen_names: set[str] = set()
    by_name: dict[str, dict] = {}
    for entry in entries:
        if not isinstance(entry, dict) or not isinstance(entry.get("name"), str) or not entry["name"]:
            raise InstallError(f"Malformed plugin entry in {catalog_path}")
        plugin_name = entry["name"]
        if plugin_name in seen_names:
            raise InstallError(f"Duplicate plugin '{plugin_name}' in {catalog_path}")
        seen_names.add(plugin_name)
        by_name[plugin_name] = entry

    for plugin_name in expected_plugins:
        entry = by_name.get(plugin_name)
        if entry is None:
            raise InstallError(f"Plugin '{plugin_name}' missing from {catalog_path}")
        source = entry.get("source")
        source_path = source.get("path") if isinstance(source, dict) else source
        if not isinstance(source_path, str) or not source_path:
            raise InstallError(f"Missing 'source' for '{plugin_name}' in {catalog_path}")
        resolved_source = (git_root / source_path).resolve()
        expected_dir = (workspace / ".agents" / "plugins" / plugin_name).resolve()
        if not resolved_source.is_dir() or resolved_source != expected_dir:
            raise InstallError(
                f"Plugin '{plugin_name}' source '{source_path}' in {catalog_path} "
                f"does not resolve to {expected_dir}"
            )
        if not isinstance(entry.get("description"), str) or not entry["description"].strip():
            raise InstallError(f"Missing 'description' for '{plugin_name}' in {catalog_path}")
        if not isinstance(entry.get("version"), str) or not entry["version"].strip():
            raise InstallError(f"Missing 'version' for '{plugin_name}' in {catalog_path}")


def verify_installation(
    target_workspace: Path,
    expected_plugins: Optional[list[str]] = None,
    *,
    include_copilot_marketplace: bool = True,
) -> list[str]:
    """Verify Phase 1 and Phase 2 installation artifacts against MARKETPLACE.MD."""
    workspace = validate_workspace(target_workspace)
    git_root = resolve_git_root(workspace)
    checks: list[str] = []

    agents_dir = workspace / ".agents"
    plugins_dir = agents_dir / "plugins"
    marketplace_path = plugins_dir / "marketplace.json"
    if not agents_dir.is_dir() or not plugins_dir.is_dir():
        raise InstallError(f"Missing .agents/plugins directory under {workspace}")
    if not marketplace_path.is_file():
        raise InstallError(f"Missing marketplace catalog: {marketplace_path}")

    marketplace = load_json_object(marketplace_path, "marketplace.json")
    marketplace_name = marketplace.get("name")
    if not isinstance(marketplace_name, str) or not marketplace_name:
        raise InstallError(f"Missing 'name' in {marketplace_path}")
    entries = marketplace.get("plugins")
    if not isinstance(entries, list):
        raise InstallError(f"Expected 'plugins' array in {marketplace_path}")

    registered_names: list[str] = []
    seen: set[str] = set()
    for entry in entries:
        if not isinstance(entry, dict) or not isinstance(entry.get("name"), str) or not entry["name"]:
            raise InstallError(f"Malformed plugin entry in {marketplace_path}")
        name = entry["name"]
        if name in seen:
            raise InstallError(f"Duplicate plugin '{name}' in {marketplace_path}")
        seen.add(name)
        registered_names.append(name)

    plugins_to_check = deduplicate_names(expected_plugins) if expected_plugins else registered_names
    if not plugins_to_check:
        raise InstallError(f"No plugins registered or specified to verify in {marketplace_path}")

    for name in plugins_to_check:
        plugin_dir = plugins_dir / name
        if not plugin_dir.is_dir():
            raise InstallError(f"Missing installed plugin directory: {plugin_dir}")
        if name not in seen:
            raise InstallError(f"Plugin '{name}' is not registered in {marketplace_path}")
    checks.append(f"Phase 1: {len(plugins_to_check)} plugin(s) installed and registered in {marketplace_path}")

    if DOTNET_PLUGIN_NAME in plugins_to_check:
        antigravity_link = plugins_dir / DOTNET_PLUGIN_NAME / "hooks.json"
        if not antigravity_link.is_symlink():
            raise InstallError(f"Antigravity hooks.json is not a symlink: {antigravity_link}")
        if os.readlink(antigravity_link) != "com.google.antigravity/hooks.json":
            raise InstallError(
                f"Antigravity symlink target mismatch at {antigravity_link}: {os.readlink(antigravity_link)}"
            )
        if not antigravity_link.resolve().is_file():
            raise InstallError(f"Broken Antigravity symlink: {antigravity_link}")
        checks.append(f"Phase 2 (antigravity): verified {antigravity_link}")

        codex_hooks_link = git_root / ".codex" / "hooks.json"
        expected_hooks_target = (plugins_dir / DOTNET_PLUGIN_NAME / "com.openai" / "hooks" / "hooks.json").resolve()
        if not codex_hooks_link.is_symlink():
            raise InstallError(f"Codex hooks.json is not a symlink: {codex_hooks_link}")
        if codex_hooks_link.resolve() != expected_hooks_target or not expected_hooks_target.is_file():
            raise InstallError(f"Codex hooks.json does not resolve to {expected_hooks_target}")
        hooks_data = load_json_object(expected_hooks_target, "Codex hooks.json")
        validate_codex_hook_commands(hooks_data, git_root, expected_hooks_target)
        checks.append(f"Phase 2 (codex hooks): verified {codex_hooks_link}")

    antigravity_agent_count = 0
    for name in plugins_to_check:
        if (plugins_dir / name / "com.google.antigravity" / "agents").is_dir():
            antigravity_agent_count += len(
                verify_client_agents(
                    plugins_dir,
                    [name],
                    "com.google.antigravity",
                    plugins_dir / name / "agents",
                    "Antigravity",
                )
            )
    checks.append(f"Phase 2 (antigravity agents): verified {antigravity_agent_count} copied agent file(s)")

    expected_agent_files = verify_client_agents(
        plugins_dir,
        plugins_to_check,
        "com.openai",
        git_root / ".codex" / "agents",
        "Codex",
    )
    checks.append(f"Phase 2 (codex agents): verified {len(expected_agent_files)} copied agent file(s)")

    codex_config_path = git_root / ".codex" / "config.toml"
    if not codex_config_path.is_file():
        raise InstallError(f"Missing Codex config: {codex_config_path}")
    try:
        codex_config = tomllib.loads(codex_config_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, tomllib.TOMLDecodeError) as exc:
        raise InstallError(f"Invalid Codex config {codex_config_path}: {exc}") from exc
    plugins_table = codex_config.get("plugins")
    if not isinstance(plugins_table, dict):
        raise InstallError(f"Missing [plugins] entries in {codex_config_path}")
    for name in plugins_to_check:
        key = f"{name}@{marketplace_name}"
        entry = plugins_table.get(key)
        if not isinstance(entry, dict) or entry.get("enabled") is not True:
            raise InstallError(f"Plugin '{key}' is not enabled in {codex_config_path}")
    checks.append(f"Phase 2 (codex config): verified {codex_config_path}")

    settings_path = git_root / ".github" / "copilot" / "settings.json"
    if not settings_path.is_file():
        raise InstallError(f"Missing Copilot settings: {settings_path}")
    settings = load_json_object(settings_path, "Copilot settings.json")
    marketplaces = settings.get("extraKnownMarketplaces")
    enabled = settings.get("enabledPlugins")
    if not isinstance(marketplaces, dict) or marketplace_name not in marketplaces:
        raise InstallError(f"Marketplace '{marketplace_name}' missing in {settings_path}")
    if not isinstance(enabled, dict):
        raise InstallError(f"Missing 'enabledPlugins' object in {settings_path}")
    for name in plugins_to_check:
        key = f"{name}@{marketplace_name}"
        if enabled.get(key) is not True:
            raise InstallError(f"Plugin '{key}' is not enabled in {settings_path}")
    checks.append(f"Phase 2 (copilot settings): verified {settings_path}")

    if include_copilot_marketplace:
        verify_copilot_marketplace(workspace, git_root, plugins_to_check)
        checks.append(f"Phase 2 (copilot marketplace): verified {git_root / '.github' / 'plugin' / 'marketplace.json'}")

    return checks


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    discover_parser = subparsers.add_parser(
        "discover",
        help="Clone/inspect source plugins and target workspace status as JSON.",
    )
    discover_parser.add_argument("target_workspace", type=Path)
    discover_parser.add_argument("--source-repo", type=Path, help="Explicit local agent-docs checkout")

    install_parser = subparsers.add_parser(
        "install",
        help="Install or update selected plugins and deterministic client configs.",
    )
    install_parser.add_argument("target_workspace", type=Path)
    install_parser.add_argument("--plugins", nargs="+", required=True, help="Plugin names to install/update")
    install_parser.add_argument("--source-repo", type=Path, help="Explicit local agent-docs checkout")

    verify_parser = subparsers.add_parser(
        "verify",
        help="Verify all Phase 1 and Phase 2 checks including .github/plugin/marketplace.json.",
    )
    verify_parser.add_argument("target_workspace", type=Path)
    verify_parser.add_argument("--plugins", nargs="+", help="Expected plugin names (defaults to marketplace.json)")
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    try:
        if args.command == "discover":
            report = discover(args.target_workspace, args.source_repo)
            print(json.dumps(report, indent=2))
        elif args.command == "install":
            checks = install(args.target_workspace, args.plugins, args.source_repo)
            for line in checks:
                print(line)
            print(
                "Next step: create or update .github/plugin/marketplace.json at the repository root, "
                "then run the 'verify' subcommand."
            )
        elif args.command == "verify":
            checks = verify_installation(args.target_workspace, args.plugins, include_copilot_marketplace=True)
            for line in checks:
                print(line)
    except InstallError as error:
        print(f"Marketplace {args.command} failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
