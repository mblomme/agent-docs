#!/usr/bin/env bash
set -euo pipefail

repo_root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)
cd -- "$repo_root"

# jq parses hook input and safely escapes command output in JSON responses.
stop_hook_active=$(jq -er '(.stop_hook_active // false) | tostring')

shopt -s nullglob
solutions=(./*.slnx ./*.sln)
if (( ${#solutions[@]} != 1 )); then
    jq -n --arg reason "Expected exactly one .slnx or .sln file in $repo_root; found ${#solutions[@]}." \
        '{continue: false, systemMessage: $reason}'
    exit 0
fi
solution=${solutions[0]}

fingerprint() {
    {
    sha256sum .codex/hooks/qa.sh .editorconfig
    find . -type d \( -name .git -o -name bin -o -name obj -o -name .codex \) -prune -o \
        -type f \( -name '*.cs' -o -name '*.csproj' -o -name '*.slnx' -o -name '*.sln' \
        -o -name '*.props' -o -name '*.targets' -o -name '*.json' \
        -o -name .editorconfig -o -name .csharpierrc -o -name NuGet.Config \) \
        -print0 | LC_ALL=C sort -z | xargs -0 -r sha256sum
    } | sha256sum | cut -d ' ' -f 1
}

cache_key=$(printf '%s' "$repo_root" | sha256sum | cut -d ' ' -f 1)
cache_dir="${TMPDIR:-/tmp}/focusflow-codex-validation-$UID"
mkdir -p -- "$cache_dir"
chmod 700 -- "$cache_dir"
cache_file="$cache_dir/$cache_key.sha256"
if [[ -f "$cache_file" && $(<"$cache_file") == "$(fingerprint)" ]]; then
    printf '{}\n'
    exit 0
fi

output_file=$(mktemp "$cache_dir/output.XXXXXX")
trap 'rm -f -- "$output_file"' EXIT

run_check() {
    if "$@" >"$output_file" 2>&1; then
        return 0
    fi

    local reason
    reason=$(printf 'Repository validation failed, fix these failures, rebuild and rerun validation before finishing: %s\n%s' "$*" "$(tail -c 16000 "$output_file")")
    if [[ "$stop_hook_active" == true ]]; then
        jq -n --arg reason "$reason" '{continue: false, systemMessage: $reason}'
    else
        jq -n --arg reason "$reason" '{decision: "block", reason: $reason}'
    fi
    # The JSON decision controls continuation; do not run subsequent checks.
    exit 0
}

run_check dotnet tool restore --tool-manifest dotnet-tools.json
run_check dotnet tool run csharpier format .
run_check dotnet build "$solution" --no-incremental -m:1 -warnaserror \
    -p:EnableNETAnalyzers=true -p:AnalysisMode=All -p:EnforceCodeStyleInBuild=true
run_check dotnet test "$solution" --no-build -m:1

fingerprint >"$cache_file"
printf '{}\n'
