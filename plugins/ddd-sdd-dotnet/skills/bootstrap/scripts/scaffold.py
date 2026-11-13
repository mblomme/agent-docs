#!/usr/bin/env python3
"""Create the basic .NET projects in the current repository."""

import argparse
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path


FOLDERS = ("src", "tests", "lib")
PROJECTS = (
    ("src", "Domain", "classlib"),
    ("src", "Application", "classlib"),
    ("src", "Infrastructure", "classlib"),
    ("src", "Main", "web"),
    ("tests", "AcceptanceTests", "xunit"),
    ("tests", "DesignTests", "xunit"),
    ("tests", "ArchitectureTests", "xunit"),
)


class ScaffoldError(Exception):
    """A conflict or failed command stopped the scaffolding."""


@dataclass(frozen=True)
class Project:
    path: Path
    template: str


def main():
    inputs = read_inputs()
    repository = Path.cwd()
    prefix = project_prefix(inputs.company, inputs.bounded_context)
    projects = plan_projects(repository, prefix)

    solution = find_existing_solution(repository)
    existing_projects = inspect_solution(repository, solution)
    check_destinations(repository, projects)

    create_basic_folders(repository)
    solution = solution or create_solution(repository, prefix)
    create_missing_projects(projects)
    add_projects_to_solution(solution, projects, existing_projects)

    print(f"Scaffold complete. Solution: {solution}")


def read_inputs():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--bounded-context", required=True, type=validate_name)
    parser.add_argument("--company", type=validate_name)
    return parser.parse_args()


def validate_name(value):
    pattern = r"[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*"
    if not re.fullmatch(pattern, value):
        raise argparse.ArgumentTypeError("use dot-separated C# identifier segments")
    return value


def project_prefix(company, bounded_context):
    return ".".join(part for part in (company, bounded_context) if part)


def plan_projects(repository, prefix):
    projects = []
    for folder, layer, template in PROJECTS:
        name = f"{prefix}.{layer}"
        projects.append(Project(repository / folder / name / f"{name}.csproj", template))
    return projects


def find_existing_solution(repository):
    solutions = sorted([*repository.glob("*.sln"), *repository.glob("*.slnx")])
    if len(solutions) > 1:
        raise ScaffoldError("Multiple root solutions found; resolve before scaffolding")
    return solutions[0] if solutions else None


def inspect_solution(repository, solution):
    if solution is None:
        run_dotnet("--version", capture=True)
        return set()

    result = run_dotnet("sln", str(solution), "list", capture=True)
    return {
        str((repository / line.strip()).resolve())
        for line in result.stdout.splitlines()
        if line.strip().lower().endswith(".csproj")
    }


def check_destinations(repository, projects):
    for folder in FOLDERS:
        path = repository / folder
        if path.exists() and not path.is_dir():
            raise ScaffoldError(f"Expected a directory: {path}")

    for project in projects:
        if project.path.is_file():
            validate_existing_project(project)
        elif destination_is_occupied(project.path):
            raise ScaffoldError(f"Project destination is occupied: {project.path.parent}")


def destination_is_occupied(path):
    if path.exists():
        return True
    directory = path.parent
    return directory.exists() and (not directory.is_dir() or any(directory.iterdir()))


def validate_existing_project(project):
    try:
        contents = ET.parse(project.path).getroot()
    except (ET.ParseError, OSError) as exc:
        raise ScaffoldError(f"Cannot read existing project {project.path}: {exc}") from exc

    expected_sdk = "Microsoft.NET.Sdk.Web" if project.template == "web" else "Microsoft.NET.Sdk"
    if contents.get("Sdk") != expected_sdk:
        raise ScaffoldError(f"Incompatible project {project.path}: expected SDK {expected_sdk}")
    if project.template == "xunit":
        validate_test_packages(project, contents)


def validate_test_packages(project, contents):
    packages = {item.get("Include", "").lower() for item in contents.iter("PackageReference")}
    if "xunit" not in packages:
        raise ScaffoldError(f"Incompatible test project {project.path}: missing xunit reference")
    if is_architecture_test_project(project) and not any("archunitnet" in package for package in packages):
        raise ScaffoldError(f"Incompatible test project {project.path}: missing ArchUnitNET reference")


def create_basic_folders(repository):
    for folder in FOLDERS:
        (repository / folder).mkdir(exist_ok=True)
    library = repository / "lib"
    if not any(library.iterdir()):
        (library / ".gitkeep").touch()


def create_solution(repository, prefix):
    run_dotnet("new", "sln", "--name", prefix, "--output", str(repository))
    solution = find_existing_solution(repository)
    if solution is None:
        raise ScaffoldError("The SDK did not create exactly one root solution")
    return solution


def create_missing_projects(projects):
    for project in projects:
        if project.path.is_file():
            continue
        create_project(project)
        remove_template_examples(project)
        if is_architecture_test_project(project):
            add_architecture_test_package(project)


def create_project(project):
    run_dotnet(
        "new", project.template,
        "--name", project.path.stem,
        "--output", str(project.path.parent),
        "--no-restore",
    )


def remove_template_examples(project):
    for filename in ("Class1.cs", "UnitTest1.cs"):
        (project.path.parent / filename).unlink(missing_ok=True)


def is_architecture_test_project(project):
    return project.path.stem.endswith(".ArchitectureTests")


def add_architecture_test_package(project):
    run_dotnet(
        "add", str(project.path), "package", "TngTech.ArchUnitNET.xUnit",
        "--version", "0.13.4", "--no-restore",
    )


def add_projects_to_solution(solution, projects, existing_projects):
    for project in projects:
        if str(project.path.resolve()) not in existing_projects:
            run_dotnet("sln", str(solution), "add", str(project.path))


def run_dotnet(*arguments, capture=False):
    command = ("dotnet", *arguments)
    try:
        return subprocess.run(command, check=True, text=True, capture_output=capture)
    except FileNotFoundError as exc:
        raise ScaffoldError("Required command not found: dotnet") from exc
    except subprocess.CalledProcessError as exc:
        detail = (exc.stderr or exc.stdout or "").strip()
        suffix = f": {detail}" if detail else ""
        raise ScaffoldError(f"Command failed: {' '.join(command)}{suffix}") from exc


if __name__ == "__main__":
    try:
        main()
    except ScaffoldError as error:
        print(f"Scaffolding stopped: {error}", file=sys.stderr)
        sys.exit(1)
