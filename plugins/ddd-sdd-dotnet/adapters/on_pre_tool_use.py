#!/usr/bin/env python3
"""Gate the first apply_patch of each Codex session on .NET quality checks."""

import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys


class HookError(Exception):
    pass


def main():
    try:
        event = json.load(sys.stdin)
        if not isinstance(event, dict):
            raise HookError("Hook input must be a JSON object.")
        session_id = event.get("session_id")
        if not isinstance(session_id, str) or not session_id:
            raise HookError("Hook event must contain a nonempty session_id.")
        workspace_root = find_workspace_root(event)
        marker = marker_path(session_id)
        try:
            descriptor = os.open(marker, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
        except FileExistsError:
            response = {}
        else:
            os.close(descriptor)
            response = run_quality_checks(workspace_root)
    except json.JSONDecodeError as error:
        response = block(f"Invalid hook JSON: {error}")
    except (HookError, OSError, UnicodeError) as error:
        response = block(f".NET pre-edit QA could not run: {error}")
    print(json.dumps(response, ensure_ascii=True))
    return 0


def find_workspace_root(event):
    workspace_path = event.get("cwd")
    if (
        not isinstance(workspace_path, str)
        or not workspace_path
        or not Path(workspace_path).is_absolute()
    ):
        raise HookError("Hook event must contain an absolute workspace directory.")
    workspace = Path(workspace_path)
    if not workspace.is_dir():
        raise HookError(f"Hook workspace does not exist: {workspace}")
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


def marker_path(session_id):
    digest = hashlib.sha256(session_id.encode("utf-8")).hexdigest()
    return f"/tmp/codex_quality_gate_{digest}.checked"


def block(reason):
    return {"decision": "block", "reason": reason}


def run_quality_checks(workspace_root):
    script_path = Path(__file__).resolve().parent.parent / "scripts" / "pre_quality_checks.sh"
    result = subprocess.run(
        ["bash", str(script_path), str(workspace_root)],
        capture_output=True,
        text=True,
        errors="replace",
        check=False,
    )
    if result.returncode == 0:
        return {}
    diagnostic = result.stderr.strip() or f".NET QA exited with status {result.returncode} without diagnostics."
    return block(
        "First-edit .NET quality checks failed:\n"
        f"{diagnostic}\n\n"
        "Ask the user for permission before editing any files. If the user approves "
        "continuing anyway, retry apply_patch; later edits in this session will pass."
    )


if __name__ == "__main__":
    sys.exit(main())
