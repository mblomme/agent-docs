from codecs import BOM_UTF8
from typing import Optional


MarkerPairs = tuple[tuple[str, str], ...]


def find_marker_bounds(
    lines: list[str], marker_pairs: MarkerPairs, description: str
) -> Optional[tuple[int, int]]:
    starts = [
        (index, pair)
        for index, line in enumerate(lines)
        for pair in marker_pairs
        if line.strip() == pair[0]
    ]
    ends = [
        (index, pair)
        for index, line in enumerate(lines)
        for pair in marker_pairs
        if line.strip() == pair[1]
    ]
    if not starts and not ends:
        return None
    if (
        len(starts) != 1
        or len(ends) != 1
        or starts[0][1] != ends[0][1]
        or starts[0][0] >= ends[0][0]
    ):
        raise ValueError(f"{description} markers are incomplete, duplicated, or out of order")
    return starts[0][0], ends[0][0]


def line_ending(text: str) -> str:
    return "\r\n" if "\r\n" in text else "\n"


def encode_like_original(text: str, original: bytes) -> bytes:
    bom = BOM_UTF8 if original.startswith(BOM_UTF8) else b""
    return bom + text.encode("utf-8")
