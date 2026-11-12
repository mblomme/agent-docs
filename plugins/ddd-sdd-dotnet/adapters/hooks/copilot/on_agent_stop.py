#!/usr/bin/env python3
"""Adapt copilot stop hooks to shared quality validation."""
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
    if outcome.attempts == MAX_RETRIES:
        reason += (f"\n\n.NET QA has reached its limit of {MAX_RETRIES} attempts. "
                   "The next stop will not rerun validation; report any unresolved failures.")
    if not outcome.state_recorded:
        reason += "\n.NET QA cannot track retries; validation has not completed."
        if active is not False:
            print(reason, file=sys.stderr)
            return {"systemMessage": reason}
        reason += " The next stop will be allowed if this error persists."
    return {"decision": "block", "reason": reason}


if __name__ == "__main__":
    main(normalize, read_active, render)
