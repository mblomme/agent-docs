"""Run the first-edit gate once for an opaque session key."""
import hashlib
import os
from .quality import ValidationError, run_quality_checks as validate_workspace

def create_session_marker(session_key):
    try:
        descriptor = os.open(marker_path(session_key), os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    except FileExistsError:
        return False
    os.close(descriptor)
    return True


def marker_path(session_key):
    digest = hashlib.sha256(session_key.encode("utf-8")).hexdigest()
    return f"/tmp/quality_gate_{digest}.checked"


def run_quality_checks(workspace_root):
    try:
        validate_workspace(workspace_root)
    except ValidationError as error:
        return f"First-edit .NET quality checks failed:\n{error}"
    return ""
