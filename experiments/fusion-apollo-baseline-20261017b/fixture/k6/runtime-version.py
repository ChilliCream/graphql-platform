#!/usr/bin/env python3
"""Print the runtime version a gateway / subgraphs directory runs on, or nothing.

Usage: runtime-version.py [--package <NuGet package id>] <component-dir>
"""

import glob
import json
import os
import re
import subprocess
import sys

SEMVER = re.compile(
    r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)"
    r"(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$"
)


def _version_key(version):
    core, _, pre = version.partition("-")
    nums = tuple(int(p) if p.isdigit() else 0 for p in core.split("."))
    # A release sorts above its prereleases.
    return nums, (pre == "", pre)


def dotnet_runtime_version(component_dir):
    shared = os.path.join(component_dir, ".dotnet", "shared", "Microsoft.NETCore.App")
    if not os.path.isdir(shared):
        return ""
    versions = [v for v in os.listdir(shared) if SEMVER.match(v)]
    return max(versions, key=_version_key) if versions else ""


def normalize_numeric_version(raw):
    """Turn a Java-style version (25, 25.0.4, 25.0.4.1, 25.0.4+7) into X.Y.Z."""
    match = re.match(r"^(\d+(?:\.\d+)*)", raw.strip())
    if not match:
        return ""
    parts = [str(int(p)) for p in match.group(1).split(".")][:3]
    parts += ["0"] * (3 - len(parts))
    return ".".join(parts)


def jvm_runtime_version(component_dir):
    for release in (
        os.path.join(component_dir, ".jdk", "release"),
        os.path.join(component_dir, ".jdk", "Contents", "Home", "release"),
    ):
        if not os.path.isfile(release):
            continue
        with open(release, encoding="utf-8", errors="replace") as fh:
            for line in fh:
                key, _, value = line.partition("=")
                if key.strip() == "JAVA_VERSION":
                    return normalize_numeric_version(value.strip().strip('"'))
    return ""


def node_runtime_version(component_dir):
    if not os.path.isfile(os.path.join(component_dir, "package.json")):
        return ""
    try:
        out = subprocess.run(
            ["node", "--version"], capture_output=True, text=True, timeout=30
        ).stdout.strip()
    except (OSError, subprocess.SubprocessError):
        return ""
    return out[1:] if out[:1] in ("v", "V") else out


def runtime_version(component_dir):
    for probe in (
        dotnet_runtime_version,
        jvm_runtime_version,
        node_runtime_version,
    ):
        version = probe(component_dir)
        if version:
            return version if SEMVER.match(version) else ""
    return ""


def package_version(component_dir, package):
    """Version of `package` in the first build output deps.json that lists it."""
    pattern = os.path.join(component_dir, "*", "bin", "Release", "*", "*.deps.json")
    for deps in sorted(glob.glob(pattern)):
        try:
            with open(deps, encoding="utf-8") as fh:
                libraries = json.load(fh).get("libraries", {})
        except (OSError, ValueError):
            continue
        for key in libraries:
            name, _, version = key.partition("/")
            if name == package and SEMVER.match(version):
                return version
    return ""


def main():
    args = sys.argv[1:]
    if len(args) == 3 and args[0] == "--package":
        print(package_version(args[2], args[1]))
        return
    if len(args) != 1:
        print("Usage: runtime-version.py [--package <id>] <component-dir>", file=sys.stderr)
        sys.exit(1)
    try:
        version = runtime_version(args[0])
    except OSError:
        version = ""
    print(version)


if __name__ == "__main__":
    main()
