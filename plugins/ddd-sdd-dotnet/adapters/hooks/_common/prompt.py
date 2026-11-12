"""Capture the workspace baseline used by stop validation."""
import json
from .quality import workspace_fingerprint


def record_baseline(root, state):
    baseline = state.with_suffix(".baseline")
    baseline.unlink(missing_ok=True)
    fingerprint = workspace_fingerprint(root)
    source = workspace_fingerprint(root, ("lib", "src"))
    if fingerprint is not None and source is not None:
        baseline.write_text(json.dumps({"all": fingerprint, "source": source}), encoding="ascii")
