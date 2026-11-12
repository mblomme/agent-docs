"""Run shared validation and return protocol-independent outcomes."""

from dataclasses import dataclass
import json
import subprocess
import sys

from .quality import (
    MAX_RETRIES, ValidationError, retry_state_path,
    read_retry_count, write_retry_count, workspace_matches_baseline, run_quality_checks,
)


@dataclass
class Outcome:
    reason: str = ""
    fatal: bool = False
    state_recorded: bool = False
    attempts: int = 0
    exhausted: bool = False


def validate(root, session_key, active):
    state = retry_state_path(root, session_key)
    if active is False:
        state.unlink(missing_ok=True)
    if active is not True and workspace_matches_baseline(state, root):
        return Outcome()
    run_tests = not workspace_matches_baseline(state, root, source_only=True)
    attempts = read_retry_count(state)
    if attempts >= MAX_RETRIES:
        state.unlink(missing_ok=True)
        return Outcome(
            reason=f".NET QA retry limit ({MAX_RETRIES} attempts) reached. "
            "Validation was not rerun; check the previous failure before finishing.",
            exhausted=True,
        )
    attempts += 1
    write_retry_count(state, attempts)
    try:
        run_quality_checks(root, run_tests)
        state.unlink(missing_ok=True)
        return Outcome()
    except ValidationError as error:
        return Outcome(str(error), error.fatal, True, attempts)
    except (OSError, UnicodeError, ValueError, subprocess.SubprocessError) as error:
        return Outcome(f".NET QA could not run: {error}", True, True, attempts)


def main(normalize, read_active, render):
    active = None
    try:
        event = json.load(sys.stdin)
        if not isinstance(event, dict):
            raise ValidationError("Hook input must be a JSON object.", fatal=True)
        active = read_active(event)
        path, session_key = normalize(event)
        outcome = validate(path, session_key, active)
    except json.JSONDecodeError as error:
        outcome = Outcome(f"Invalid hook JSON: {error}", True)
    except ValidationError as error:
        outcome = Outcome(str(error), error.fatal)
    except (OSError, UnicodeError, ValueError, subprocess.SubprocessError) as error:
        outcome = Outcome(f".NET QA could not run: {error}", True)
    print(json.dumps(render(outcome, active), ensure_ascii=True))
