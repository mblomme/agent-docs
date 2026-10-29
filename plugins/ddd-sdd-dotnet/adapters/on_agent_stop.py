#!/usr/bin/env python3
"""Translate native hook events and decisions to shared .NET validation."""

import json
from pathlib import Path
import subprocess
import sys

CLIENTS = {"codex", "copilot"}


class ValidationError(Exception):
    def __init__(self, message, *, fatal=False):
        super().__init__(message)
        self.fatal = fatal


def main():
    if len(sys.argv) != 2 or sys.argv[1] not in CLIENTS:
        print("Usage: on_agent_stop.py {codex|copilot}", file=sys.stderr)
        return 2
    client = sys.argv[1]
    stop_hook_active = False
    try:
        event = read_hook_event()
        stop_hook_active = stop_hook_is_active(event)
        workspace_root = find_workspace_root(event)
        run_quality_checks(workspace_root)
        response = {}
    except json.JSONDecodeError as error:
        response = failure_response(client, f"Invalid hook JSON: {error}", stop_hook_active, True)
    except ValidationError as error:
        response = failure_response(client, str(error), stop_hook_active, error.fatal)
    except (OSError, UnicodeError) as error:
        response = failure_response(client, f".NET QA could not run: {error}", stop_hook_active, True)
    print(json.dumps(response, ensure_ascii=True))
    return 0


def read_hook_event():
    event = json.load(sys.stdin)
    if not isinstance(event, dict):
        raise ValidationError("Hook input must be a JSON object.", fatal=True)
    return event


def stop_hook_is_active(event):
    active = event.get("stop_hook_active", False)
    if not isinstance(active, bool):
        raise ValidationError("stop_hook_active must be a boolean.", fatal=True)
    return active


def find_workspace_root(event):
    workspace_path = event.get("cwd")
    if (
        not isinstance(workspace_path, str)
        or not workspace_path
        or not Path(workspace_path).is_absolute()
    ):
        raise ValidationError("Hook event must contain an absolute workspace directory.", fatal=True)

    workspace = Path(workspace_path)
    if not workspace.is_dir():
        raise ValidationError(f"Hook workspace does not exist: {workspace}", fatal=True)

    try:
        result = subprocess.run(
            ["git", "-C", str(workspace), "rev-parse", "--show-toplevel"],
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            text=True,
            check=False,
        )
    except FileNotFoundError:
        return workspace.resolve()

    if result.returncode == 0:
        return Path(result.stdout.strip()).resolve()
    return workspace.resolve()


def failure_response(client, reason, stop_hook_active, fatal):
    if client == "copilot":
        return {"decision": "block", "reason": reason}
    if stop_hook_active or fatal:
        return {"continue": False, "systemMessage": reason}
    return {"decision": "block", "reason": reason}


def run_quality_checks(workspace_root):
    script_path = Path(__file__).resolve().parent.parent / "scripts" / "quality_checks.sh"
    result = subprocess.run(
        ["bash", str(script_path), str(workspace_root)],
        capture_output=True,
        text=True,
        errors="replace",
        check=False,
    )
    if result.returncode == 0:
        return

    message = result.stderr.strip()
    if not message:
        message = f".NET QA exited with status {result.returncode} without diagnostics."
    raise ValidationError(message, fatal=result.returncode != 1)


if __name__ == "__main__":
    sys.exit(main())
