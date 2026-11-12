#!/usr/bin/env python3
"""Adapt codex first-edit hooks to shared quality checks."""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from _common.quality import ValidationError
from _common.pre_edit import create_session_marker, run_quality_checks
from inputs import normalize


def main():
    reason = ""
    try:
        event = json.load(sys.stdin)
        if not isinstance(event, dict):
            raise ValidationError("Hook input must be a JSON object.")
        root, session = normalize(event)
        if create_session_marker(session):
            reason = run_quality_checks(root)
            if reason:
                reason += (
                    "\n\nAsk the user for permission before editing any files. "
                    "If the user approves continuing anyway, retry the edit; "
                    "later edits in this session will pass."
                )
    except json.JSONDecodeError as error:
        reason = f"Invalid hook JSON: {error}"
    except (ValidationError, OSError, UnicodeError) as error:
        reason = f".NET pre-edit QA could not run: {error}"
    response = {"decision": "block", "reason": reason} if reason else {}
    print(json.dumps(response, ensure_ascii=True))


if __name__ == "__main__":
    main()
