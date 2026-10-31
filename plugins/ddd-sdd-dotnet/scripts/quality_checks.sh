#!/usr/bin/env bash
set -euo pipefail

main() {
    resolve_repository "$@"
    find_solution
    check_requirements

    output_file=$(mktemp "${TMPDIR:-/tmp}/ddd-sdd-dotnet-output.XXXXXX") ||
        fail 2 "Cannot create validation output file."
    trap cleanup_temporary_file EXIT

    run_quality_checks
}

fail() {
    local exit_code=$1
    shift
    printf '%s\n' "$*" >&2
    exit "$exit_code"
}

resolve_repository() {
    if (( $# != 1 )); then
        fail 2 "Expected one target repository directory."
    fi

    repo_root=$(cd -- "$1" && pwd -P) || fail 2 "Cannot access target directory: $1"
    cd -- "$repo_root"
}

find_solution() {
    shopt -s nullglob
    local solutions=(./*.slnx ./*.sln)
    if (( ${#solutions[@]} != 1 )); then
        fail 2 "Expected exactly one .slnx or .sln file in $repo_root; found ${#solutions[@]}."
    fi
    solution=${solutions[0]}
}

check_requirements() {
    local required_file
    for required_file in .editorconfig dotnet-tools.json; do
        [[ -f "$required_file" ]] || fail 1 "Required file is missing: $repo_root/$required_file"
    done
    command -v dotnet >/dev/null || fail 1 "Required command not found: dotnet"
}

cleanup_temporary_file() {
    rm -f -- "$output_file"
}

run_check() {
    if "$@" >"$output_file" 2>&1; then
        return
    fi
    printf 'Repository validation failed, FIX these failures, MUST REBUILD AND RERUN validation before finishing: %s\n' "$*" >&2
    tail -c 16000 "$output_file" >&2
    exit 1
}

run_quality_checks() {
    run_check dotnet tool restore --tool-manifest dotnet-tools.json
    run_check dotnet tool run csharpier format .
    run_check dotnet build "$solution" --no-incremental -m:1 -warnaserror \
        -p:EnableNETAnalyzers=true -p:AnalysisMode=All -p:EnforceCodeStyleInBuild=true
    run_check dotnet test "$solution" --no-build -m:1
}

main "$@"
