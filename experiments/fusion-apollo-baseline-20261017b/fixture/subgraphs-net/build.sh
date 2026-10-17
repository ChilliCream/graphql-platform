#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

REQUIRED_DOTNET_MAJOR=10
CSPROJ_FILES=()
while IFS= read -r csproj; do
  [[ -n "$csproj" ]] && CSPROJ_FILES+=("$csproj")
done < <(find "$SCRIPT_DIR" -mindepth 2 -maxdepth 2 -type f -name '*.csproj' | sort)

if [[ ${#CSPROJ_FILES[@]} -eq 0 ]]; then
  echo "ERROR: no .csproj files found under $SCRIPT_DIR"
  exit 1
fi

# --- Verify pinned HotChocolate version ---
# Subgraph packages stay on a pinned HotChocolate release: build.sh never
# resolves versions from NuGet and never rewrites tracked files. To move to a
# newer release, bump PINNED_HOTCHOCOLATE_VERSION alongside the csproj
# PackageReferences in the same commit.
#
# This check runs before any SDK cleanup or download so a version mismatch
# fails fast without touching the bundled runtime or requiring network access.
# References are parsed as XML (xml.etree.ElementTree), so attribute order,
# line breaks, extra attributes, and single- or double-quoted attributes are
# all handled, while commented-out PackageReferences are ignored.
PINNED_HOTCHOCOLATE_VERSION="16.6.8"
echo "Verifying HotChocolate package references are pinned to $PINNED_HOTCHOCOLATE_VERSION..."
PINNED="$PINNED_HOTCHOCOLATE_VERSION" python3 - "${CSPROJ_FILES[@]}" <<'PYEOF'
import os
import sys
import xml.etree.ElementTree as ET

pinned = os.environ["PINNED"]
failures = []
for csproj in sys.argv[1:]:
    try:
        tree = ET.parse(csproj)
    except ET.ParseError as exc:
        failures.append(f"{csproj}: invalid XML ({exc})")
        continue
    root = tree.getroot()
    seen = False
    for elem in root.iter():
        tag = elem.tag
        if not isinstance(tag, str):
            continue
        local = tag.rsplit("}", 1)[-1] if "}" in tag else tag
        if local != "PackageReference":
            continue
        include = elem.get("Include", "")
        if not include.startswith("HotChocolate"):
            continue
        seen = True
        version = elem.get("Version")
        if version is None:
            for child in elem:
                ctag = child.tag
                if not isinstance(ctag, str):
                    continue
                clocal = ctag.rsplit("}", 1)[-1] if "}" in ctag else ctag
                if clocal == "Version":
                    version = (child.text or "").strip()
                    break
        else:
            version = version.strip()
        if version != pinned:
            failures.append(
                f"{csproj}: {include} version "
                f"'{version if version is not None else 'none'}' is not pinned to {pinned}"
            )
    if seen and not any(f.startswith(csproj + ":") for f in failures):
        print(f"  Pinned: {os.path.basename(os.path.dirname(csproj))}/{os.path.basename(csproj)}")
if failures:
    for failure in failures:
        print(f"ERROR: {failure}")
    sys.exit(1)
PYEOF

# --- Install the .NET SDK, bundled into the subgraphs dir ---------------------
# The benchmark machines have no .NET installed and never execute this script:
# they run the *prebuilt* artifact. The artifact itself must therefore carry
# the runtime the subgraphs are measured on — otherwise whatever SDK happens
# to be on the machine would silently decide the result. This mirrors the
# Fusion gateway's install.sh: the SDK lives inside the subgraphs directory,
# where it travels with the artifact.
#
# Generated here (all gitignored):
#   - $SCRIPT_DIR/.dotnet      bundled SDK (start.sh runs the apphosts on it)
#   - $SCRIPT_DIR/global.json  pins the bundled SDK (a nearer global.json takes
#                              precedence over the repo-root one)
BUNDLED_DOTNET_DIR="$SCRIPT_DIR/.dotnet"
SUBGRAPHS_GLOBAL_JSON="$SCRIPT_DIR/global.json"

# Clear the previous generated inputs up front, before anything that can fail:
# a download that dies half way then leaves a neutral tree instead of one
# whose global.json pins an SDK that is no longer installed. Both are
# (re)written below on success. CI uses fresh checkouts; local runs may reuse
# the tree.
echo "Installing the .NET $REQUIRED_DOTNET_MAJOR SDK into $BUNDLED_DOTNET_DIR ..."
rm -rf "$BUNDLED_DOTNET_DIR"
rm -f "$SUBGRAPHS_GLOBAL_JSON"
# No --quality: dotnet-install.sh then resolves the latest GA patch of the band.
curl -sSL https://dot.net/v1/dotnet-install.sh \
  | bash -s -- --channel "$REQUIRED_DOTNET_MAJOR.0" --install-dir "$BUNDLED_DOTNET_DIR"

export DOTNET_ROOT="$BUNDLED_DOTNET_DIR"
export PATH="$BUNDLED_DOTNET_DIR:$PATH"
DOTNET="$BUNDLED_DOTNET_DIR/dotnet"

# --list-sdks ignores global.json and (with multi-level lookup off) only lists
# SDKs in the bundled dir, so this is the version we actually installed.
SDK_VERSION="$("$DOTNET" --list-sdks | awk '{print $1}' | sort -V | tail -n1)"
SDK_MAJOR="${SDK_VERSION%%.*}"
if [[ "$SDK_MAJOR" != "$REQUIRED_DOTNET_MAJOR" ]]; then
  echo "ERROR: subgraphs require a .NET $REQUIRED_DOTNET_MAJOR SDK but installed '${SDK_VERSION:-none}'"
  exit 1
fi
echo "Bundled .NET SDK: $SDK_VERSION"

# Pin the bundled SDK so the selected runtime is used at build *and* run time.
cat > "$SUBGRAPHS_GLOBAL_JSON" <<EOF
{
  "sdk": {
    "version": "$SDK_VERSION",
    "rollForward": "latestMinor",
    "allowPrerelease": false
  }
}
EOF

# --- Build all subgraph projects with the bundled SDK ---
# `dotnet build` emits a framework-dependent apphost next to each dll
# (bin/Release/net10.0/<ProjectName>); start.sh launches those apphosts on the
# bundled runtime, so the benchmark machine needs no .NET of its own.
echo "Building subgraphs..."
for csproj in "${CSPROJ_FILES[@]}"; do
  project_name="$(basename "$(dirname "$csproj")")"
  echo "  Building $project_name..."
  "$DOTNET" build "$csproj" -c Release --nologo -v quiet
done

echo "Subgraphs build complete (HotChocolate $PINNED_HOTCHOCOLATE_VERSION)."
