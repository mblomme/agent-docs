from dataclasses import dataclass
from pathlib import Path
import re
from typing import Iterator
from xml.dom import Node, minidom
from xml.parsers import expat

from style_helpers import encode_like_original, find_marker_bounds, line_ending


START = "<!-- BEGIN ddd-sdd-dotnet install-code-style -->"
END = "<!-- END ddd-sdd-dotnet install-code-style -->"
MARKERS = (
    (START, END),
    ("<!-- BEGIN ddd-sdd-dotnet conf-code-style -->",
     "<!-- END ddd-sdd-dotnet conf-code-style -->"),
)
MSBUILD_NAMESPACE = "http://schemas.microsoft.com/developer/msbuild/2003"


@dataclass(frozen=True)
class RequiredProperty:
    name: str
    value: str


@dataclass(frozen=True)
class PropsTemplate:
    contents: bytes
    group_xml: str
    properties: dict[str, RequiredProperty]


def parse_project(data: bytes, description: str) -> minidom.Document:
    data.decode("utf-8-sig")
    parser = expat.ParserCreate()

    def reject_doctype(*_args):
        raise ValueError(f"{description} must not contain a DOCTYPE")

    parser.StartDoctypeDeclHandler = reject_doctype
    try:
        parser.Parse(data, True)
        document = minidom.parseString(data)
    except expat.ExpatError as error:
        raise ValueError(f"invalid XML in {description}: {error}") from error

    if document.encoding and document.encoding.lower().replace("-", "") != "utf8":
        raise ValueError(f"{description} must use UTF-8 encoding")
    root = document.documentElement
    if root.localName != "Project" or root.namespaceURI not in (None, MSBUILD_NAMESPACE):
        raise ValueError(f"{description} must have an MSBuild Project root")
    return document


def child_elements(node: Node) -> list[minidom.Element]:
    return [child for child in node.childNodes if isinstance(child, minidom.Element)]


def property_value(element: minidom.Element, description: str) -> str:
    if child_elements(element):
        raise ValueError(f"{description} has a complex {element.localName} value")
    return "".join(
        child.data for child in element.childNodes
        if isinstance(child, minidom.Text)
    ).strip()


def load_template(path: Path) -> PropsTemplate:
    contents = path.read_bytes()
    document = parse_project(contents, "Directory.Build.props reference")
    groups = child_elements(document.documentElement)
    if len(groups) != 1 or groups[0].localName != "PropertyGroup":
        raise ValueError("Directory.Build.props reference must contain one PropertyGroup")

    properties = {}
    for element in child_elements(groups[0]):
        key = element.localName.casefold()
        if key in properties or element.hasAttribute("Condition"):
            raise ValueError("invalid property in Directory.Build.props reference")
        properties[key] = RequiredProperty(element.localName, property_value(element, "reference"))

    nowarn = properties.get("nowarn")
    if nowarn is None or not nowarn.value.startswith("$(NoWarn);"):
        raise ValueError("Directory.Build.props reference must append to NoWarn")
    if len(properties) < 2 or any(not prop.value for prop in properties.values()):
        raise ValueError("Directory.Build.props reference has missing property values")
    return PropsTemplate(contents, groups[0].toxml(), properties)


def validate_existing_properties(root: minidom.Element, template: PropsTemplate) -> None:
    for element in root.getElementsByTagName("*"):
        if element.namespaceURI != root.namespaceURI:
            continue
        name = element.localName.casefold()
        parent = element.parentNode.localName.casefold()
        if (
            name in ("import", "importgroup")
            and parent in ("project", "importgroup", "when", "otherwise")
        ):
            raise ValueError(
                "Directory.Build.props contains imports that cannot be safely checked for conflicts"
            )
        if parent != "propertygroup" or name == "nowarn" or name not in template.properties:
            continue
        required = template.properties[name]
        actual = property_value(element, "Directory.Build.props")
        if actual.casefold() != required.value.casefold():
            raise ValueError(
                f"Directory.Build.props conflict for {required.name}: "
                f"expected {required.value!r}, found {actual!r}"
            )


def comments_in(node: Node) -> Iterator[minidom.Comment]:
    for child in node.childNodes:
        if isinstance(child, minidom.Comment):
            yield child
        yield from comments_in(child)


def validate_managed_nodes(root: minidom.Element, markers: list[minidom.Comment]) -> None:
    if any(marker.parentNode is not root for marker in markers):
        raise ValueError("Directory.Build.props managed markers must be direct Project children")
    children = list(root.childNodes)
    between = children[children.index(markers[0]) + 1:children.index(markers[1])]
    elements = [node for node in between if isinstance(node, minidom.Element)]
    has_text = any(
        isinstance(node, minidom.Text) and node.data.strip()
        for node in between
    )
    if len(elements) != 1 or elements[0].localName != "PropertyGroup" or has_text:
        raise ValueError("Directory.Build.props managed block has unexpected content")


def remove_managed_block(text: str, root: minidom.Element) -> str:
    lines = text.splitlines(keepends=True)
    bounds = find_marker_bounds(lines, MARKERS, "Directory.Build.props managed")
    marker_texts = {
        marker.removeprefix("<!--").removesuffix("-->").strip()
        for pair in MARKERS for marker in pair
    }
    comments = [comment for comment in comments_in(root) if comment.data.strip() in marker_texts]
    if len(comments) != (2 if bounds is not None else 0):
        raise ValueError("Directory.Build.props managed markers must occupy their own lines")
    if bounds is None:
        return text

    validate_managed_nodes(root, comments)
    start, end = bounds
    return "".join(lines[:start] + lines[end + 1:])


def root_end_offset(data: bytes) -> int:
    parser = expat.ParserCreate()
    depth = 0
    offset = None

    def start(_name, _attrs):
        nonlocal depth
        depth += 1

    def finish(_name):
        nonlocal depth, offset
        depth -= 1
        if depth == 0:
            offset = parser.CurrentByteIndex

    parser.StartElementHandler = start
    parser.EndElementHandler = finish
    parser.Parse(data, True)
    if offset is None:
        raise ValueError("cannot locate the Directory.Build.props Project closing tag")
    return offset


def managed_block(group_xml: str, prefix: str, newline: str) -> bytes:
    if prefix:
        group_xml = re.sub(
            r"(<\/?)([A-Za-z_][\w.-]*)(?=[\s/>])",
            lambda match: match.group(1) + prefix + ":" + match.group(2),
            group_xml,
        )
    group_xml = group_xml.replace("\n", newline)
    return ("  " + START + newline + "  " + group_xml + newline
            + "  " + END + newline).encode("utf-8")


def insert_managed_block(data: bytes, root: minidom.Element, block: bytes, newline: str) -> bytes:
    # Byte offsets preserve the original XML formatting and any UTF-8 BOM.
    offset = root_end_offset(data)
    if data[offset:offset + 2] == b"</":
        line_start = data.rfind(b"\n", 0, offset) + 1
        if data[line_start:offset].strip():
            return data[:offset] + newline.encode() + block + data[offset:]
        return data[:line_start] + block + data[line_start:]
    if data[offset - 2:offset] != b"/>":
        raise ValueError("cannot locate the Directory.Build.props Project closing tag")
    closing = f"</{root.tagName}>".encode("utf-8")
    return data[:offset - 2] + b">" + newline.encode() + block + closing + data[offset:]


def update_props(original: bytes, template: PropsTemplate) -> bytes:
    document = parse_project(original, "Directory.Build.props")
    root = document.documentElement
    validate_existing_properties(root, template)
    if original == template.contents:
        return original

    text = original.decode("utf-8-sig")
    text = remove_managed_block(text, root)
    data = encode_like_original(text, original)
    parse_project(data, "Directory.Build.props without managed block")
    newline = line_ending(text)
    block = managed_block(template.group_xml, root.prefix or "", newline)
    return insert_managed_block(data, root, block, newline)
