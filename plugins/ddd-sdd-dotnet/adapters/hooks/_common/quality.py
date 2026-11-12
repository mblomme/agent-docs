#!/usr/bin/env python3
"""Filesystem state and quality checks for normalized hook requests."""

import hashlib
import json
import os
from pathlib import Path
import stat
import shutil
import subprocess
import tempfile

MAX_RETRIES = 3


class ValidationError(Exception):
    def __init__(self, message, *, fatal=False):
        super().__init__(message)
        self.fatal = fatal


def workspace_matches_baseline(state_file, workspace_root, source_only=False):
    baseline = state_file.with_suffix(".baseline")
    if not baseline.exists():
        return False
    try:
        expected = json.loads(baseline.read_text(encoding="ascii"))
        expected = expected["source" if source_only else "all"]
    except (OSError, UnicodeError, ValueError, KeyError, TypeError):
        return False
    folders = ("lib", "src") if source_only else ("lib", "src", "tests")
    actual = workspace_fingerprint(workspace_root, folders)
    return actual is not None and actual == expected


def retry_state_path(workspace_root, session_key):
    directory = Path(tempfile.gettempdir()) / f"ddd-sdd-dotnet-stop-{os.getuid()}"
    directory.mkdir(mode=0o700, exist_ok=True)
    info = directory.lstat()
    if not stat.S_ISDIR(info.st_mode) or info.st_uid != os.getuid() or info.st_mode & 0o077:
        raise ValidationError(f"Insecure .NET QA retry state directory: {directory}", fatal=True)
    key = json.dumps([str(workspace_root), session_key], ensure_ascii=True)
    return directory / f"{hashlib.sha256(key.encode('utf-8')).hexdigest()}.retries"


def read_retry_count(state_file):
    try:
        flags = os.O_RDONLY | os.O_NOFOLLOW
        with os.fdopen(os.open(state_file, flags), encoding="utf-8") as state:
            count = int(state.read())
    except FileNotFoundError:
        return 0
    if not 0 <= count <= MAX_RETRIES:
        raise ValidationError("Invalid .NET QA retry count.", fatal=True)
    return count


def write_retry_count(state_file, retry_count):
    flags = os.O_WRONLY | os.O_CREAT | os.O_TRUNC | os.O_NOFOLLOW
    descriptor = os.open(state_file, flags, 0o600)
    with os.fdopen(descriptor, "w", encoding="utf-8") as state:
        state.write(str(retry_count))


def find_workspace_root(workspace_path):
    if (
        not isinstance(workspace_path, str)
        or not workspace_path
        or not Path(workspace_path).is_absolute()
    ):
        raise ValidationError("Hook event must contain an absolute workspace directory.", fatal=True)

    workspace = Path(workspace_path)
    if not workspace.is_dir():
        raise ValidationError(f"Hook workspace does not exist: {workspace}", fatal=True)

    try:
        result = subprocess.run(
            ["git", "-C", str(workspace), "rev-parse", "--show-toplevel"],
            stdout=subprocess.PIPE,
            stderr=subprocess.DEVNULL,
            text=True,
            check=False,
        )
    except FileNotFoundError:
        return workspace.resolve()

    if result.returncode == 0:
        return Path(result.stdout.strip()).resolve()
    return workspace.resolve()


def workspace_fingerprint(workspace_root, folders=("lib", "src", "tests")):
    """Hash the selected trees without following symbolic links."""
    try:
        digest = hashlib.sha256()
        for folder in folders:
            root = workspace_root / folder
            add_path_to_fingerprint(digest, root, workspace_root)
            if root.is_dir() and not root.is_symlink():
                for current, directories, files in os.walk(
                    root, followlinks=False, onerror=raise_walk_error
                ):
                    directories.sort()
                    files.sort()
                    for name in directories + files:
                        add_path_to_fingerprint(digest, Path(current) / name, workspace_root)
        return digest.hexdigest()
    except OSError:
        return None


def raise_walk_error(error):
    raise error


def add_path_to_fingerprint(digest, path, workspace_root):
    name = os.fsencode(str(path.relative_to(workspace_root)))
    digest.update(len(name).to_bytes(8, "big"))
    digest.update(name)
    if path.is_symlink():
        target = os.fsencode(os.readlink(path))
        digest.update(b"link" + len(target).to_bytes(8, "big") + target)
    elif path.is_dir():
        digest.update(b"directory")
    elif path.is_file():
        digest.update(b"file")
        digest.update(file_fingerprint(path))
    else:
        digest.update(b"other" if path.exists() else b"missing")


def file_fingerprint(path):
    content = hashlib.sha256()
    with path.open("rb") as file:
        for chunk in iter(lambda: file.read(1024 * 1024), b""):
            content.update(chunk)
    return content.digest()


def run_quality_checks(workspace_root, run_tests=True):
    """Run .NET checks in order, retaining a bounded diagnostic tail on failure."""
    solutions = sorted((*workspace_root.glob("*.slnx"), *workspace_root.glob("*.sln")))
    if len(solutions) != 1:
        raise ValidationError(
            f"Expected exactly one .slnx or .sln file in {workspace_root}; found {len(solutions)}.",
            fatal=True,
        )
    for name in (".editorconfig", "dotnet-tools.json"):
        if not (workspace_root / name).is_file():
            raise ValidationError(f"Required file is missing: {workspace_root / name}")
    if shutil.which("dotnet") is None:
        raise ValidationError("Required command not found: dotnet")

    solution = "./" + solutions[0].name
    commands = [
        ["dotnet", "tool", "restore", "--tool-manifest", "dotnet-tools.json"],
        ["dotnet", "tool", "run", "csharpier", "format", "."],
        ["dotnet", "build", solution, "--no-incremental", "-m:1", "-warnaserror",
         "-p:EnableNETAnalyzers=true", "-p:AnalysisMode=All", "-p:EnforceCodeStyleInBuild=true"],
    ]
    if run_tests:
        commands.append(["dotnet", "test", solution, "--no-build", "-m:1"])
    try:
        output = tempfile.TemporaryFile()
    except OSError as error:
        raise ValidationError(f"Cannot create validation output file: {error}", fatal=True) from error
    with output:
        for command in commands:
            output.seek(0)
            output.truncate()
            result = subprocess.run(
                command, cwd=workspace_root, stdout=output, stderr=subprocess.STDOUT, check=False,
            )
            if result.returncode:
                size = output.tell()
                output.seek(max(0, size - 16000))
                diagnostic = output.read().decode("utf-8", errors="replace").strip()
                raise ValidationError(
                    f"Repository validation failed: {' '.join(command)}\n{diagnostic}"
                )
