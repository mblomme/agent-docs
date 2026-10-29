from pathlib import Path

from style_helpers import encode_like_original, find_marker_bounds, line_ending


START = "# BEGIN ddd-sdd-dotnet install-code-style"
END = "# END ddd-sdd-dotnet install-code-style"
MARKERS = (
    (START, END),
    ("# BEGIN ddd-sdd-dotnet conf-code-style", "# END ddd-sdd-dotnet conf-code-style"),
)


def load_rules(path: Path) -> str:
    rules = path.read_text(encoding="utf-8")
    contains_marker = any(marker in rules for pair in MARKERS for marker in pair)
    if not rules.endswith("\n") or contains_marker:
        raise ValueError("invalid editorconfig reference")

    has_section = False
    for line in rules.splitlines():
        line = line.strip()
        if not line or line.startswith(("#", ";")):
            continue
        if line.startswith("[") and line.endswith("]") and len(line) > 2:
            has_section = True
        elif not has_section or "=" not in line or not line.split("=", 1)[0].strip():
            raise ValueError("invalid editorconfig reference")
    if not has_section:
        raise ValueError("invalid editorconfig reference")
    return rules


def preserve_trailing_section(after: str, old_block: list[str], newline: str) -> str:
    for line in after.splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith(("#", ";")):
            continue
        if stripped.startswith("["):
            return after

        sections = [
            line.strip()
            for line in old_block
            if line.strip().startswith("[") and line.strip().endswith("]")
        ]
        if not sections:
            raise ValueError("install-code-style block has no section for following properties")
        return sections[-1] + newline + after
    return after


def update_editorconfig(original: bytes, rules: str) -> bytes:
    text = original.decode("utf-8-sig")
    newline = line_ending(text)
    lines = text.splitlines(keepends=True)
    bounds = find_marker_bounds(lines, MARKERS, "install-code-style")

    if bounds is not None:
        start, end = bounds
        before = "".join(lines[:start])
        after = "".join(lines[end + 1:])
        after = preserve_trailing_section(after, lines[start + 1:end], newline)
        text = before + after

    if text and not text.endswith(("\n", "\r")):
        text += newline
    if text and not text.endswith(newline * 2):
        text += newline
    managed_block = (START + "\n" + rules + END + "\n").replace("\n", newline)
    return encode_like_original(text + managed_block, original)
