#!/usr/bin/env python3
"""Record a source folder fingerprint at the start of a user turn."""

import json
import sys

from on_agent_stop import (
    find_workspace_root,
    get_session_id,
    retry_state_path,
    workspace_fingerprint,
)


def main():
    try:
        event = json.load(sys.stdin)
        client = sys.argv[1]
        root = find_workspace_root(event)
        state = retry_state_path(client, root, get_session_id(event)).with_suffix(".baseline")
        state.unlink(missing_ok=True)
        fingerprint = workspace_fingerprint(root)
        if fingerprint is not None:
            state.write_text(fingerprint, encoding="ascii")
    except Exception:
        # Missing or unreadable baselines cause the stop hook to run checks.
        pass
    print("{}")


if __name__ == "__main__":
    main()
