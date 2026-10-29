from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
SKILL = ROOT / "skills/install-code-style"
SCRIPT = SKILL / "scripts/configure_code_style.py"
START = "# BEGIN ddd-sdd-dotnet install-code-style"
END = "# END ddd-sdd-dotnet install-code-style"
PROPS_START = "<!-- BEGIN ddd-sdd-dotnet install-code-style -->"
PROPS_END = "<!-- END ddd-sdd-dotnet install-code-style -->"
LEGACY_START = "# BEGIN ddd-sdd-dotnet conf-code-style"
LEGACY_END = "# END ddd-sdd-dotnet conf-code-style"
LEGACY_PROPS_START = "<!-- BEGIN ddd-sdd-dotnet conf-code-style -->"
LEGACY_PROPS_END = "<!-- END ddd-sdd-dotnet conf-code-style -->"
EXPECTED = {
    "*.cs": {
        "dotnet_diagnostic.CA1716.severity": "none",
        "dotnet_diagnostic.CA2007.severity": "none",
        "dotnet_diagnostic.CA1030.severity": "none",
        "dotnet_diagnostic.CA1040.severity": "none",
        "dotnet_code_quality.CA1716.analyzed_symbol_kinds": "NamedType, Method, Property, Parameter",
        "dotnet_diagnostic.IDE0290.severity": "warning",
    },
    "src/*.Domain/**/*.cs": {
        "dotnet_diagnostic.CA1030.severity": "none",
    },
    "tests/**/*.cs": {
        "dotnet_diagnostic.CA1707.severity": "none",
        "dotnet_diagnostic.CA1034.severity": "none",
        "dotnet_diagnostic.CA1515.severity": "none",
    },
    "src/**/*.cs": {
        "dotnet_diagnostic.IDE0005.severity": "warning",
        "dotnet_diagnostic.IDE0044.severity": "warning",
        "dotnet_diagnostic.IDE0051.severity": "warning",
        "dotnet_diagnostic.IDE0052.severity": "warning",
        "dotnet_diagnostic.IDE0060.severity": "suggestion",
    },
}


def parse_block(text):
    block = text.split(START, 1)[1].split(END, 1)[0]
    sections = {}
    section = None
    for line in block.splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        if line.startswith("[") and line.endswith("]"):
            section = line[1:-1]
            if section in sections:
                raise AssertionError(f"duplicate section: {section}")
            sections[section] = {}
        else:
            key, value = (part.strip() for part in line.split("=", 1))
            if key in sections[section]:
                raise AssertionError(f"duplicate key: {section}: {key}")
            sections[section][key] = value
    return sections


class CodeStyleTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="code style test ")
        self.addCleanup(self.temporary.cleanup)
        self.workspace = Path(self.temporary.name)
        self.config = self.workspace / ".editorconfig"
        self.props = self.workspace / "Directory.Build.props"

    def run_skill(self, directory=None, script=SCRIPT, cwd=None):
        return subprocess.run(
            [sys.executable, str(script), str(directory or self.workspace)],
            text=True,
            capture_output=True,
            cwd=cwd,
        )

    def test_skill_metadata_and_creation_are_complete_and_idempotent(self):
        metadata = (SKILL / "SKILL.md").read_text(encoding="utf-8")
        self.assertTrue(metadata.startswith("---\nname: install-code-style\n"))
        self.assertIn("\ndescription:", metadata)

        result = self.run_skill()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Created:", result.stdout)
        self.assertIn(f"Created: {self.props}", result.stdout)
        first = self.config.read_bytes()
        first_props = self.props.read_bytes()
        self.assertEqual(first_props, (SKILL / "references/Directory.Build.props").read_bytes())
        self.assertEqual(parse_block(first.decode("utf-8")), EXPECTED)
        self.assertEqual(first.count(START.encode()), 1)
        self.assertEqual(first.count(END.encode()), 1)
        result = self.run_skill()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Already up to date:", result.stdout)
        self.assertIn(f"Already up to date: {self.props}", result.stdout)
        self.assertEqual(self.config.read_bytes(), first)
        self.assertEqual(self.props.read_bytes(), first_props)

    def test_merges_props_preserving_content_bom_and_crlf(self):
        original = (
            b'\xef\xbb\xbf<?xml version="1.0" encoding="utf-8"?>\r\n'
            b'<Project>\r\n  <!-- user comment -->\r\n'
            b'  <PropertyGroup Condition="\'$(Configuration)\' == \'Debug\'">\r\n'
            b'    <CustomProperty>keep me</CustomProperty>\r\n'
            b'    <NoWarn>$(NoWarn);CS0168</NoWarn>\r\n'
            b'  </PropertyGroup>\r\n</Project>\r\n'
        )
        self.props.write_bytes(original)
        result = self.run_skill(cwd=self.workspace)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn(f"Updated: {self.props}", result.stdout)
        merged = self.props.read_bytes()
        self.assertTrue(merged.startswith(original[:original.index(b"</Project>")]))
        self.assertTrue(merged.endswith(b"</Project>\r\n"))
        self.assertNotIn(b"\n", merged.replace(b"\r\n", b""))
        root = ET.fromstring(merged)
        managed = root.findall("PropertyGroup")[-1]
        self.assertEqual(managed.findtext("EnableNETAnalyzers"), "true")
        self.assertEqual(managed.findtext("EnforceCodeStyleInBuild"), "true")
        self.assertEqual(managed.findtext("GenerateDocumentationFile"), "true")
        self.assertEqual(managed.findtext("NoWarn"), "$(NoWarn);CS1591")
        self.assertEqual(merged.count(PROPS_START.encode()), 1)
        self.assertEqual(merged.count(PROPS_END.encode()), 1)
        result = self.run_skill()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn(f"Already up to date: {self.props}", result.stdout)
        self.assertEqual(self.props.read_bytes(), merged)

    def test_merges_empty_and_namespaced_project_roots(self):
        for original in (
            b"<Project/>",
            b'<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003"></Project>',
            b'<msb:Project xmlns:msb="http://schemas.microsoft.com/developer/msbuild/2003"/>',
        ):
            with self.subTest(original=original):
                self.props.write_bytes(original)
                result = self.run_skill()
                self.assertEqual(result.returncode, 0, result.stderr)
                merged = self.props.read_bytes()
                root = ET.fromstring(merged)
                namespace = root.tag.partition("}")[0] + "}" if "}" in root.tag else ""
                self.assertEqual(root.findtext(f"{namespace}PropertyGroup/{namespace}NoWarn"),
                                 "$(NoWarn);CS1591")
                self.assertEqual(self.run_skill().returncode, 0)
                self.assertEqual(self.props.read_bytes(), merged)

    def test_preserves_cdata_in_existing_props(self):
        original = (
            b"<Project><PropertyGroup>"
            b"<EnableNETAnalyzers><![CDATA[true]]></EnableNETAnalyzers>"
            b"<NoWarn><![CDATA[$(NoWarn);CS0168]]></NoWarn>"
            b"</PropertyGroup></Project>"
        )
        self.props.write_bytes(original)
        result = self.run_skill()
        self.assertEqual(result.returncode, 0, result.stderr)
        merged = self.props.read_bytes()
        self.assertIn(b"<EnableNETAnalyzers><![CDATA[true]]></EnableNETAnalyzers>", merged)
        self.assertIn(b"<NoWarn><![CDATA[$(NoWarn);CS0168]]></NoWarn>", merged)
        self.assertEqual(self.run_skill().returncode, 0)
        self.assertEqual(self.props.read_bytes(), merged)

    def test_reports_props_conflicts_before_changing_either_file(self):
        for name in ("EnableNETAnalyzers", "EnforceCodeStyleInBuild", "GenerateDocumentationFile"):
            for value in ("false", "$(AnalyzerSetting)", ""):
                with self.subTest(name=name, value=value):
                    config = b"root = true\n"
                    props = (
                        '<Project><PropertyGroup Condition="\'$(Configuration)\' == \'Debug\'">'
                        f"<{name}>{value}</{name}>"
                        "</PropertyGroup></Project>"
                    ).encode()
                    self.config.write_bytes(config)
                    self.props.write_bytes(props)
                    result = self.run_skill()
                    self.assertNotEqual(result.returncode, 0)
                    self.assertIn(f"conflict for {name}", result.stderr)
                    self.assertEqual(self.config.read_bytes(), config)
                    self.assertEqual(self.props.read_bytes(), props)

    def test_rejects_unsafe_props_without_creating_editorconfig(self):
        for props in (
            b"<Project>",
            b"<NotProject/>",
            b"<!DOCTYPE Project [<!ENTITY x 'value'>]><Project/>",
            b'<Project><Import Project="other.props"/></Project>',
            b"<Project>\n" + PROPS_START.encode() + b"\n</Project>\n",
            (b"<Project><PropertyGroup>\n" + PROPS_START.encode()
             + b"\n<PropertyGroup/>\n" + PROPS_END.encode()
             + b"\n</PropertyGroup></Project>"),
            (b"<Project>\n" + PROPS_START.encode() + b"\n<PropertyGroup/>\n"
             + b"<PropertyGroup/>\n" + PROPS_END.encode() + b"\n</Project>"),
            (b"<Project>\n" + PROPS_START.encode() + b"\n<PropertyGroup/>\n"
             + b"<![CDATA[unexpected]]>\n" + PROPS_END.encode() + b"\n</Project>"),
            (b"<Project>\n" + LEGACY_PROPS_START.encode() + b"\n<PropertyGroup/>\n"
             + PROPS_END.encode() + b"\n</Project>"),
            b"<Project>" + PROPS_START.encode() + PROPS_END.encode() + b"</Project>",
            b"\xff",
        ):
            with self.subTest(props=props):
                self.props.write_bytes(props)
                result = self.run_skill()
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("Cannot configure code style", result.stderr)
                self.assertFalse(self.config.exists())
                self.assertEqual(self.props.read_bytes(), props)

    def test_moves_managed_props_after_later_user_settings(self):
        self.props.write_text("<Project>\n</Project>\n", encoding="utf-8")
        self.assertEqual(self.run_skill().returncode, 0)
        original = self.props.read_text(encoding="utf-8")
        self.props.write_text(
            original.replace(
                "</Project>",
                "  <PropertyGroup><NoWarn>$(NoWarn);CS0168</NoWarn></PropertyGroup>\n"
                "</Project>",
            ),
            encoding="utf-8",
        )
        result = self.run_skill()
        self.assertEqual(result.returncode, 0, result.stderr)
        merged = self.props.read_text(encoding="utf-8")
        self.assertLess(merged.index("CS0168"), merged.index(PROPS_START))
        self.assertEqual(self.run_skill().returncode, 0)
        self.assertEqual(self.props.read_text(encoding="utf-8"), merged)

    def test_migrates_legacy_markers_in_both_files(self):
        self.props.write_text("<Project>\n</Project>\n", encoding="utf-8")
        self.assertEqual(self.run_skill().returncode, 0)
        config = self.config.read_text(encoding="utf-8")
        props = self.props.read_text(encoding="utf-8")
        self.config.write_text(config.replace(START, LEGACY_START).replace(END, LEGACY_END),
                               encoding="utf-8")
        self.props.write_text(
            props.replace(PROPS_START, LEGACY_PROPS_START).replace(PROPS_END, LEGACY_PROPS_END),
            encoding="utf-8",
        )
        result = self.run_skill()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.config.read_text(encoding="utf-8"), config)
        self.assertEqual(self.props.read_text(encoding="utf-8"), props)
        self.assertEqual(self.run_skill().returncode, 0)
        self.assertEqual(self.config.read_text(encoding="utf-8"), config)
        self.assertEqual(self.props.read_text(encoding="utf-8"), props)

    def test_rejects_mixed_and_duplicate_legacy_markers(self):
        for contents in (
            LEGACY_START + "\n" + END + "\n",
            START + "\n" + LEGACY_END + "\n",
            START + "\n" + END + "\n" + LEGACY_START + "\n" + LEGACY_END + "\n",
        ):
            with self.subTest(contents=contents):
                self.config.write_text(contents, encoding="utf-8")
                result = self.run_skill()
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("markers", result.stderr)
                self.assertEqual(self.config.read_text(encoding="utf-8"), contents)
                self.assertFalse(self.props.exists())

    def test_invalid_editorconfig_does_not_create_props(self):
        self.config.write_text(START + "\n", encoding="utf-8")
        result = self.run_skill()
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(self.props.exists())
        self.assertEqual(self.config.read_text(encoding="utf-8"), START + "\n")

    def test_invalid_references_leave_both_files_unchanged(self):
        for reference, contents in (
            ("editorconfig", None),
            ("editorconfig", "not a valid rule\n"),
            ("Directory.Build.props", None),
            ("Directory.Build.props", "<Project>"),
        ):
            with self.subTest(reference=reference, contents=contents):
                copied = self.workspace / "copied-skill"
                if copied.exists():
                    shutil.rmtree(copied)
                shutil.copytree(SKILL, copied)
                path = copied / "references" / reference
                if contents is None:
                    path.unlink()
                else:
                    path.write_text(contents, encoding="utf-8")
                self.config.write_bytes(b"root = true\n")
                self.props.write_bytes(b"<Project/>\n")
                result = self.run_skill(script=copied / "scripts/configure_code_style.py")
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("Cannot configure code style", result.stderr)
                self.assertEqual(self.config.read_bytes(), b"root = true\n")
                self.assertEqual(self.props.read_bytes(), b"<Project/>\n")

    @unittest.skipUnless(shutil.which("dotnet"), "dotnet SDK is unavailable")
    def test_msbuild_evaluates_merged_properties_and_preserves_nowarn(self):
        self.props.write_text(
            "<Project>\n"
            '  <PropertyGroup Condition="\'$(Configuration)\' == \'Debug\'">\n'
            "    <NoWarn>$(NoWarn);CS0168</NoWarn>\n"
            "  </PropertyGroup>\n</Project>\n",
            encoding="utf-8",
        )
        result = self.run_skill()
        self.assertEqual(result.returncode, 0, result.stderr)
        project = self.workspace / "check.proj"
        project.write_text(
            '<Project><Import Project="Directory.Build.props"/>'
            '<Target Name="Inspect"><Message Importance="high" '
            'Text="PROPS=$(EnableNETAnalyzers)|$(EnforceCodeStyleInBuild)|'
            '$(GenerateDocumentationFile)|$(NoWarn)"/></Target></Project>',
            encoding="utf-8",
        )
        for configuration, expected_existing in (("Debug", 1), ("Release", 0)):
            with self.subTest(configuration=configuration):
                result = subprocess.run(
                    ["dotnet", "msbuild", str(project), "-nologo", "-target:Inspect",
                     f"-property:Configuration={configuration}"],
                    cwd=self.workspace,
                    text=True,
                    capture_output=True,
                )
                self.assertEqual(result.returncode, 0, result.stderr + result.stdout)
                line = next(line.strip() for line in result.stdout.splitlines()
                            if line.strip().startswith("PROPS="))
                analyzers, style, docs, nowarn = line.removeprefix("PROPS=").split("|")
                self.assertEqual((analyzers, style, docs), ("true", "true", "true"))
                self.assertEqual(nowarn.split(";").count("CS0168"), expected_existing)
                self.assertEqual(nowarn.split(";").count("CS1591"), 1)

    @unittest.skipUnless(shutil.which("dotnet"), "dotnet SDK is unavailable")
    def test_build_reports_unused_import_when_props_are_configured(self):
        version = subprocess.run(
            ["dotnet", "--version"], text=True, capture_output=True, check=True
        ).stdout.strip()
        major = int(version.split(".", 1)[0])
        if major < 6:
            self.skipTest("test requires a .NET 6 or later SDK")
        self.assertEqual(self.run_skill().returncode, 0)
        template = self.props.read_bytes()
        self.props.unlink()
        (self.workspace / "NuGet.Config").write_text(
            "<configuration><packageSources><clear/></packageSources></configuration>",
            encoding="utf-8",
        )
        (self.workspace / "check.csproj").write_text(
            f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            f'<TargetFramework>net{major}.0</TargetFramework>'
            "</PropertyGroup></Project>",
            encoding="utf-8",
        )
        source = self.workspace / "src" / "Application"
        source.mkdir(parents=True)
        (source / "Example.cs").write_text(
            "using System.Text;\nnamespace Example;\npublic class ExampleClass { }\n",
            encoding="utf-8",
        )
        command = ["dotnet", "build", "check.csproj", "--nologo", "--no-incremental"]
        before = subprocess.run(command, cwd=self.workspace, text=True, capture_output=True)
        self.assertEqual(before.returncode, 0, before.stderr + before.stdout)
        self.assertNotIn("warning IDE0005", before.stdout + before.stderr)
        self.props.write_bytes(template)
        after = subprocess.run(command + ["--no-restore"], cwd=self.workspace,
                               text=True, capture_output=True)
        self.assertEqual(after.returncode, 0, after.stderr + after.stdout)
        self.assertIn("warning IDE0005", after.stdout + after.stderr)
        self.assertNotIn("warning CS1591", after.stdout + after.stderr)

    def test_preserves_existing_content_bom_and_crlf(self):
        existing = (
            b"\xef\xbb\xbfroot = true\r\n\r\n[*.cs]\r\n"
            b"dotnet_diagnostic.CA1716.severity = warning\r\n"
            b"indent_size = 4"
        )
        self.config.write_bytes(existing)
        result = self.run_skill()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Updated:", result.stdout)
        first = self.config.read_bytes()
        self.assertTrue(first.startswith(existing + b"\r\n\r\n"))
        self.assertNotIn(b"\n", first.replace(b"\r\n", b""))
        self.assertEqual(parse_block(first.decode("utf-8-sig")), EXPECTED)
        self.assertEqual(self.run_skill().returncode, 0)
        self.assertEqual(self.config.read_bytes(), first)

    def test_moves_managed_block_after_later_sections(self):
        self.assertEqual(self.run_skill().returncode, 0)
        with self.config.open("ab") as config:
            config.write(b"\n[*.cs]\ndotnet_diagnostic.CA2007.severity = warning\n")
        result = self.run_skill()
        self.assertEqual(result.returncode, 0, result.stderr)
        updated = self.config.read_text(encoding="utf-8")
        self.assertLess(updated.index("dotnet_diagnostic.CA2007.severity = warning"), updated.index(START))
        self.assertEqual(parse_block(updated), EXPECTED)
        self.assertEqual(self.run_skill().returncode, 0)
        self.assertEqual(self.config.read_text(encoding="utf-8"), updated)

    def test_repairs_a_modified_managed_block(self):
        self.assertEqual(self.run_skill().returncode, 0)
        original = self.config.read_text(encoding="utf-8")
        self.config.write_text(
            original.replace("dotnet_diagnostic.IDE0290.severity = warning",
                             "dotnet_diagnostic.IDE0290.severity = none"),
            encoding="utf-8",
        )
        result = self.run_skill()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Updated:", result.stdout)
        self.assertEqual(self.config.read_text(encoding="utf-8"), original)
        self.assertEqual(self.run_skill().returncode, 0)
        self.assertEqual(self.config.read_text(encoding="utf-8"), original)

    def test_preserves_section_for_properties_added_after_managed_block(self):
        self.assertEqual(self.run_skill().returncode, 0)
        with self.config.open("ab") as config:
            config.write(b"# user setting\nindent_size = 2\n")
        self.assertEqual(self.run_skill().returncode, 0)
        updated = self.config.read_text(encoding="utf-8")
        self.assertIn("[src/**/*.cs]\n# user setting\nindent_size = 2\n", updated)
        self.assertEqual(self.run_skill().returncode, 0)
        self.assertEqual(self.config.read_text(encoding="utf-8"), updated)

    def test_preserves_the_actual_final_section_of_the_old_managed_block(self):
        self.assertEqual(self.run_skill().returncode, 0)
        original = self.config.read_text(encoding="utf-8")
        self.config.write_text(
            original.replace("[src/**/*.cs]", "[other/**/*.cs]") + "indent_size = 2\n",
            encoding="utf-8",
        )
        result = self.run_skill()
        self.assertEqual(result.returncode, 0, result.stderr)
        updated = self.config.read_text(encoding="utf-8")
        self.assertIn("[other/**/*.cs]\nindent_size = 2\n", updated)
        self.assertEqual(self.run_skill().returncode, 0)
        self.assertEqual(self.config.read_text(encoding="utf-8"), updated)

    def test_invalid_markers_do_not_modify_the_file(self):
        for contents in (
            START + "\n",
            END + "\n",
            END + "\n" + START + "\n",
            START + "\n" + END + "\n" + START + "\n" + END + "\n",
        ):
            with self.subTest(contents=contents):
                self.config.write_text(contents, encoding="utf-8")
                result = self.run_skill()
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("markers", result.stderr)
                self.assertEqual(self.config.read_text(encoding="utf-8"), contents)

    def test_reports_invalid_target_and_encoding(self):
        result = self.run_skill(self.workspace / "missing")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("target directory does not exist", result.stderr)
        self.config.write_bytes(b"\xff")
        result = self.run_skill()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Cannot configure code style", result.stderr)
        self.assertEqual(self.config.read_bytes(), b"\xff")


if __name__ == "__main__":
    unittest.main()
