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
        if client == "antigravity" and event.get("invocationNum") != 0:
            print("{}")
            return
        root = find_workspace_root(event)
        retry_state = retry_state_path(client, root, get_session_id(event))
        if client == "antigravity" and retry_state.exists():
            print("{}")
            return
        state = retry_state.with_suffix(".baseline")
        state.unlink(missing_ok=True)
        fingerprint = workspace_fingerprint(root)
        source_fingerprint = workspace_fingerprint(root, ("lib", "src"))
        if fingerprint is not None and source_fingerprint is not None:
            state.write_text(json.dumps({"all": fingerprint, "source": source_fingerprint}), encoding="ascii")
    except Exception:
        # Missing or unreadable baselines cause the stop hook to run checks.
        pass
    print("{}")


if __name__ == "__main__":
    main()
