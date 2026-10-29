#!/usr/bin/env python3
import argparse
from dataclasses import dataclass
from pathlib import Path
import sys
from typing import Optional

from build_props import load_template, update_props
from editorconfig_rules import load_rules, update_editorconfig


REFERENCES = Path(__file__).resolve().parents[1] / "references"


@dataclass(frozen=True)
class PendingUpdate:
    path: Path
    original: Optional[bytes]
    contents: bytes

    def write(self) -> tuple[str, Path]:
        if self.original == self.contents:
            return "Already up to date", self.path
        self.path.write_bytes(self.contents)
        status = "Created" if self.original is None else "Updated"
        return status, self.path


def read_existing(path: Path) -> Optional[bytes]:
    return path.read_bytes() if path.exists() else None


def prepare_updates(directory: Path) -> tuple[PendingUpdate, PendingUpdate]:
    rules = load_rules(REFERENCES / "editorconfig")
    template = load_template(REFERENCES / "Directory.Build.props")

    editorconfig = directory / ".editorconfig"
    original_config = read_existing(editorconfig)
    updated_config = update_editorconfig(original_config or b"", rules)

    props = directory / "Directory.Build.props"
    original_props = read_existing(props)
    updated_props = (
        update_props(original_props, template)
        if original_props is not None else template.contents
    )
    return (
        PendingUpdate(editorconfig, original_config, updated_config),
        PendingUpdate(props, original_props, updated_props),
    )


def configure(directory: Path) -> list[tuple[str, Path]]:
    if not directory.is_dir():
        raise ValueError(f"target directory does not exist: {directory}")
    updates = prepare_updates(directory)
    return [update.write() for update in updates]


def main() -> int:
    parser = argparse.ArgumentParser(description="Configure .NET code-style rules.")
    parser.add_argument("target_directory", type=Path, help="Target repository root")
    args = parser.parse_args()
    try:
        results = configure(args.target_directory)
    except (OSError, UnicodeError, ValueError) as error:
        print(f"Cannot configure code style: {error}", file=sys.stderr)
        return 1
    for result, path in results:
        print(f"{result}: {path}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
