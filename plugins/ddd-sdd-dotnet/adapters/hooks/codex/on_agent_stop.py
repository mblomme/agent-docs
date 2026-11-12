#!/usr/bin/env python3
"""Adapt codex stop hooks to shared quality validation."""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from _common.stop import main
from _common.quality import MAX_RETRIES
from inputs import normalize, read_active


def render(outcome, active):
    if not outcome.reason:
        return {}
    if outcome.exhausted:
        print(outcome.reason, file=sys.stderr)
        return {"systemMessage": outcome.reason}
    reason = outcome.reason
    if outcome.attempts == MAX_RETRIES and not outcome.fatal:
        reason += (f"\n\n.NET QA has reached its limit of {MAX_RETRIES} attempts. "
                   "The next stop will not rerun validation; report any unresolved failures.")
    if outcome.fatal:
        return {"continue": False, "systemMessage": reason}
    return {"decision": "block", "reason": reason}


if __name__ == "__main__":
    main(normalize, read_active, render)
