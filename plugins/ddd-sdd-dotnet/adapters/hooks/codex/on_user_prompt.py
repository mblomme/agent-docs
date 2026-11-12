#!/usr/bin/env python3
"""Capture the baseline for a codex user turn."""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from _common.quality import retry_state_path
from _common.prompt import record_baseline
from inputs import normalize


def main():
    try:
        event = json.load(sys.stdin)
        root, session = normalize(event)
        state = retry_state_path(root, session)
        record_baseline(root, state)
    except Exception:
        # Without a readable baseline, stop validation runs all checks.
        pass
    print("{}")


if __name__ == "__main__":
    main()
