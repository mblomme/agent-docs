"""Normalize antigravity hook inputs."""
import json
from _common.quality import ValidationError, find_workspace_root


def normalize(event):
    path = event.get("cwd")
    if path is None:
        paths = event.get("workspacePaths")
        if not isinstance(paths, list) or len(paths) != 1:
            raise ValidationError("Hook event must contain exactly one workspace path.", fatal=True)
        path = paths[0]
    root = find_workspace_root(path)
    session = event.get("conversationId")
    if not isinstance(session, str) or not session:
        raise ValidationError("Hook event must contain a nonempty session_id or sessionId.", fatal=True)
    return root, json.dumps(["antigravity", session])


def read_active(event):
    active = event.get("stop_hook_active")
    if "stop_hook_active" in event and not isinstance(active, bool):
        raise ValidationError("stop_hook_active must be a boolean.", fatal=True)
    return active
