"""Normalize copilot hook inputs."""
import json
from _common.quality import ValidationError, find_workspace_root


def normalize(event):
    path = event.get("cwd")
    root = find_workspace_root(path)
    session = event.get("sessionId", event.get("session_id"))
    if not isinstance(session, str) or not session:
        raise ValidationError("Hook event must contain a nonempty session_id or sessionId.", fatal=True)
    return root, json.dumps(["copilot", session])


def read_active(event):
    active = event.get("stop_hook_active")
    if "stop_hook_active" in event and not isinstance(active, bool):
        raise ValidationError("stop_hook_active must be a boolean.", fatal=True)
    return active
