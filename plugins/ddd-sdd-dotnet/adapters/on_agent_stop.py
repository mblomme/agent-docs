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

    response = handle_stop(sys.argv[1])
    print(json.dumps(response, ensure_ascii=True))
    return 0


def handle_stop(client):
    stop_hook_active = None
    try:
        event = read_hook_event()
        stop_hook_active = stop_hook_is_active(event)
        return validate_stop(client, event, stop_hook_active)
    except json.JSONDecodeError as error:
        return failure_response(client, f"Invalid hook JSON: {error}", True, stop_hook_active, False)
    except ValidationError as error:
        return failure_response(client, str(error), error.fatal, stop_hook_active, False)
    except (OSError, UnicodeError, ValueError, subprocess.SubprocessError) as error:
        return failure_response(client, f".NET QA could not run: {error}", True, stop_hook_active, False)


def validate_stop(client, event, stop_hook_active):
    workspace_root = find_workspace_root(event)
    state_file = retry_state_path(client, workspace_root, get_session_id(event))
    if stop_hook_active is False:
        state_file.unlink(missing_ok=True)
    if stop_hook_active is not True and workspace_matches_baseline(state_file, workspace_root):
        return {}
    return validate_workspace(client, workspace_root, state_file, stop_hook_active)


def validate_workspace(client, workspace_root, state_file, stop_hook_active):
    retry_count = read_retry_count(state_file)
    if retry_count >= MAX_RETRIES:
        return retry_limit_response(state_file)

    retry_count += 1
    write_retry_count(state_file, retry_count)
    try:
        run_quality_checks(workspace_root)
        state_file.unlink(missing_ok=True)
        return {}
    except ValidationError as error:
        return validation_failure_response(client, error, retry_count, stop_hook_active)
    except (OSError, UnicodeError, ValueError, subprocess.SubprocessError) as error:
        return failure_response(client, f".NET QA could not run: {error}", True, stop_hook_active, True)


def validation_failure_response(client, error, retry_count, stop_hook_active):
    reason = str(error)
    if retry_count == MAX_RETRIES and not (client == "codex" and error.fatal):
        reason += (
            f"\n\n.NET QA has reached its limit of {MAX_RETRIES} attempts. "
            "The next stop will not rerun validation; report any unresolved failures."
        )
    return failure_response(client, reason, error.fatal, stop_hook_active, True)


def workspace_matches_baseline(state_file, workspace_root):
    baseline = state_file.with_suffix(".baseline")
    if not baseline.exists():
        return False
    expected = baseline.read_text(encoding="ascii")
    actual = workspace_fingerprint(workspace_root)
    return actual is not None and actual == expected


def retry_limit_response(state_file):
    state_file.unlink(missing_ok=True)
    message = (
        f".NET QA retry limit ({MAX_RETRIES} attempts) reached. "
        "Validation was not rerun; check the previous failure before finishing."
    )
    print(message, file=sys.stderr)
    return {"systemMessage": message}


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


def workspace_fingerprint(workspace_root):
    """Hash the lib, src, and tests trees without following symbolic links."""
    try:
        digest = hashlib.sha256()
        for folder in ("lib", "src", "tests"):
            root = workspace_root / folder
            add_path_to_fingerprint(digest, root, workspace_root)
            if root.is_dir() and not root.is_symlink():
                for current, directories, files in os.walk(
                    root, followlinks=False, onerror=raise_walk_error
                ):
                    directories.sort()
                    files.sort()
                    for name in directories + files:
                        add_path_to_fingerprint(digest, Path(current) / name, workspace_root)
        return digest.hexdigest()
    except OSError:
        return None


def raise_walk_error(error):
    raise error


def add_path_to_fingerprint(digest, path, workspace_root):
    name = os.fsencode(str(path.relative_to(workspace_root)))
    digest.update(len(name).to_bytes(8, "big"))
    digest.update(name)
    if path.is_symlink():
        target = os.fsencode(os.readlink(path))
        digest.update(b"link" + len(target).to_bytes(8, "big") + target)
    elif path.is_dir():
        digest.update(b"directory")
    elif path.is_file():
        digest.update(b"file")
        digest.update(file_fingerprint(path))
    else:
        digest.update(b"other" if path.exists() else b"missing")


def file_fingerprint(path):
    content = hashlib.sha256()
    with path.open("rb") as file:
        for chunk in iter(lambda: file.read(1024 * 1024), b""):
            content.update(chunk)
    return content.digest()


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
