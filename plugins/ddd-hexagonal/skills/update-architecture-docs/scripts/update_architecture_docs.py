#!/usr/bin/env python3
"""Replace spec/architecture from agent-docs and refresh its index section."""

import argparse
from contextlib import contextmanager
from pathlib import Path
import shutil
import subprocess
import tempfile


REPOSITORY = "https://github.com/mblomme/agent-docs.git"
START = "<!-- update-architecture-docs:start -->"
END = "<!-- update-architecture-docs:end -->"


@contextmanager
def source_repository(explicit):
    if explicit is not None:
        yield explicit.resolve()
        return
    for parent in Path(__file__).resolve().parents:
        if (parent / "architecture/llms.txt").is_file():
            yield parent
            return
    with tempfile.TemporaryDirectory(prefix="agent-docs-source-") as temporary:
        checkout = Path(temporary) / "agent-docs"
        subprocess.run(
            ["git", "clone", "--depth", "1", REPOSITORY, str(checkout)],
            check=True,
        )
        yield checkout


def index_section(source):
    lines = [
        START,
        "## Architecture",
        "",
        "Canonical Ports & Adapters (Hexagonal) and DDD architecture reference.",
        "Read the architecture index for document selection and dependency rules.",
        "",
        "- [Architecture index](architecture/llms.txt)",
    ]
    for document in sorted(source.rglob("*.md")):
        title = next(
            (line[2:].strip() for line in document.read_text(encoding="utf-8").splitlines()
             if line.startswith("# ")),
            document.stem,
        )
        title = title.replace("[", r"\[").replace("]", r"\]")
        relative = document.relative_to(source).as_posix()
        lines.append(f"- [{title}](<architecture/{relative}>)")
    return "\n".join([*lines, END])


def updated_index(existing, section):
    if START in existing or END in existing:
        if existing.count(START) != 1 or existing.count(END) != 1:
            raise ValueError("Malformed managed architecture section in spec/llms.txt")
        begin = existing.index(START)
        end = existing.index(END)
        if end < begin:
            raise ValueError("Reversed architecture section markers in spec/llms.txt")
        return existing[:begin] + section + existing[end + len(END):]
    prefix = existing.rstrip() or "# Specification index"
    return prefix + "\n\n" + section + "\n"


def update(target, repository):
    source = (repository / "architecture").resolve()
    if not (source / "llms.txt").is_file():
        raise ValueError(f"Source architecture index is missing: {source / 'llms.txt'}")
    if not target.is_dir():
        raise ValueError(f"Target repository directory does not exist: {target}")
    spec = target / "spec"
    destination = spec / "architecture"
    index = spec / "llms.txt"
    if any(path.is_symlink() for path in (spec, destination, index)):
        raise ValueError("spec, spec/architecture, and spec/llms.txt must not be symlinks")
    if destination.exists() and not destination.is_dir():
        raise ValueError(f"Architecture destination is not a directory: {destination}")
    resolved_destination = destination.resolve()
    if (source == resolved_destination or source in resolved_destination.parents
            or resolved_destination in source.parents):
        raise ValueError("Source and destination architecture directories must not overlap")
    existing = index.read_text(encoding="utf-8") if index.exists() else ""
    content = updated_index(existing, index_section(source))
    spec.mkdir(exist_ok=True)
    # Stage on the same filesystem so directory renames are atomic. Retain the old
    # tree until both the replacement and the index have been installed.
    with tempfile.TemporaryDirectory(prefix=".architecture-update-", dir=spec) as temporary:
        staging = Path(temporary)
        replacement = staging / "architecture"
        backup = staging / "previous"
        shutil.copytree(source, replacement, ignore=shutil.ignore_patterns(".git"))
        next_index = staging / "llms.txt"
        next_index.write_text(content, encoding="utf-8")
        had_destination = destination.exists()
        if had_destination:
            destination.rename(backup)
        try:
            replacement.rename(destination)
            next_index.replace(index)
        except OSError:
            if destination.exists():
                shutil.rmtree(destination)
            if had_destination:
                backup.rename(destination)
            raise
    print(f"Architecture source: {source}")
    print(f"Replaced: {destination}")
    print(f"Updated: {index}")
    print("Review older architecture entries outside the managed index section.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("target_repository", type=Path)
    parser.add_argument("--source-repo", type=Path, help="Local agent-docs checkout")
    args = parser.parse_args()
    try:
        with source_repository(args.source_repo) as repository:
            update(args.target_repository.resolve(), repository)
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        parser.exit(1, f"Architecture update failed: {error}\n")


if __name__ == "__main__":
    main()
