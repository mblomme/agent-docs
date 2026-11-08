#!/usr/bin/env python3
"""Translate native hook events and decisions to shared .NET validation."""

import hashlib
import json
import os
from pathlib import Path
import stat
import subprocess
import sys
import tempfile

CLIENTS = {"codex", "copilot"}
MAX_RETRIES = 3


class ValidationError(Exception):
    def __init__(self, message, *, fatal=False):
        super().__init__(message)
        self.fatal = fatal


def main():
    if len(sys.argv) != 2 or sys.argv[1] not in CLIENTS:
        print("Usage: on_agent_stop.py {codex|copilot}", file=sys.stderr)
        return 2
    client = sys.argv[1]
    stop_hook_active = None
    retry_count = None
    state_recorded = False
    try:
        event = read_hook_event()
        stop_hook_active = stop_hook_is_active(event)
        workspace_root = find_workspace_root(event)
        session_id = get_session_id(event)
        state_file = retry_state_path(client, workspace_root, session_id)
        if stop_hook_active is False:
            state_file.unlink(missing_ok=True)
        retry_count = read_retry_count(state_file)
        if retry_count >= MAX_RETRIES:
            state_file.unlink(missing_ok=True)
            message = (
                f".NET QA retry limit ({MAX_RETRIES} attempts) reached. "
                "Validation was not rerun; check the previous failure before finishing."
            )
            print(message, file=sys.stderr)
            response = {"systemMessage": message}
        else:
            retry_count += 1
            write_retry_count(state_file, retry_count)
            state_recorded = True
            run_quality_checks(workspace_root)
            state_file.unlink(missing_ok=True)
            response = {}
    except json.JSONDecodeError as error:
        response = failure_response(client, f"Invalid hook JSON: {error}", True, stop_hook_active, state_recorded)
    except ValidationError as error:
        reason = str(error)
        if retry_count == MAX_RETRIES and not (client == "codex" and error.fatal):
            reason += (
                f"\n\n.NET QA has reached its limit of {MAX_RETRIES} attempts. "
                "The next stop will not rerun validation; report any unresolved failures."
            )
        response = failure_response(client, reason, error.fatal, stop_hook_active, state_recorded)
    except (OSError, UnicodeError, ValueError, subprocess.SubprocessError) as error:
        response = failure_response(
            client, f".NET QA could not run: {error}", True, stop_hook_active, state_recorded
        )
    print(json.dumps(response, ensure_ascii=True))
    return 0


def read_hook_event():
    event = json.load(sys.stdin)
    if not isinstance(event, dict):
        raise ValidationError("Hook input must be a JSON object.", fatal=True)
    return event


def stop_hook_is_active(event):
    if "stop_hook_active" not in event:
        return None
    active = event["stop_hook_active"]
    if not isinstance(active, bool):
        raise ValidationError("stop_hook_active must be a boolean.", fatal=True)
    return active


def get_session_id(event):
    session_id = event.get("session_id", event.get("sessionId"))
    if not isinstance(session_id, str) or not session_id:
        raise ValidationError("Hook event must contain a nonempty session_id or sessionId.", fatal=True)
    return session_id


def retry_state_path(client, workspace_root, session_id):
    directory = Path(tempfile.gettempdir()) / f"ddd-sdd-dotnet-stop-{os.getuid()}"
    directory.mkdir(mode=0o700, exist_ok=True)
    info = directory.lstat()
    if not stat.S_ISDIR(info.st_mode) or info.st_uid != os.getuid() or info.st_mode & 0o077:
        raise ValidationError(f"Insecure .NET QA retry state directory: {directory}", fatal=True)
    key = json.dumps([client, str(workspace_root), session_id], ensure_ascii=True)
    digest = hashlib.sha256(key.encode("utf-8")).hexdigest()
    return directory / f"{digest}.retries"


def read_retry_count(state_file):
    try:
        flags = os.O_RDONLY | os.O_NOFOLLOW
        with os.fdopen(os.open(state_file, flags), encoding="utf-8") as state:
            count = int(state.read())
    except FileNotFoundError:
        return 0
    if not 0 <= count <= MAX_RETRIES:
        raise ValidationError("Invalid .NET QA retry count.", fatal=True)
    return count


def write_retry_count(state_file, retry_count):
    flags = os.O_WRONLY | os.O_CREAT | os.O_TRUNC | os.O_NOFOLLOW
    descriptor = os.open(state_file, flags, 0o600)
    with os.fdopen(descriptor, "w", encoding="utf-8") as state:
        state.write(str(retry_count))


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


def failure_response(client, reason, fatal, stop_hook_active, state_recorded):
    if client == "copilot":
        if not state_recorded:
            reason += "\n.NET QA cannot track retries; validation has not completed."
            if stop_hook_active is not False:
                print(reason, file=sys.stderr)
                return {"systemMessage": reason}
            reason += " The next stop will be allowed if this error persists."
        return {"decision": "block", "reason": reason}
    if fatal:
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
