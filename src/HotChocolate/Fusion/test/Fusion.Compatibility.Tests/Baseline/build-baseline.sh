#!/usr/bin/env bash
# Rebuilds the pinned 16.6 baseline consumer artifact committed under Baseline/artifacts.
#
# The baseline consumer (BaselineConsumer/) is intentionally isolated from the repository's
# central build/package configuration (its own Directory.Build.props / Directory.Packages.props
# stop the MSBuild property walk-up) so that it only ever restores the pinned 16.6.4 NuGet
# packages named in BaselineConsumer.csproj, never the local, in-repo product source.
#
# This script is run out of band, by hand, whenever the pinned baseline needs to be regenerated
# (for example, to pin a newer 16.6.x patch). It is never invoked as part of building or testing
# HotChocolate.Fusion.Compatibility.Tests: the compiled artifact it produces is committed to the
# repository so the compatibility test harness always loads the same, known-provenance bytes.
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
consumer_dir="$script_dir/BaselineConsumer"
artifacts_dir="$script_dir/artifacts"
assembly_name="HotChocolate.Fusion.Compatibility.BaselineConsumer"

rm -rf "$consumer_dir/bin" "$consumer_dir/obj"
dotnet build "$consumer_dir/BaselineConsumer.csproj" -c Release

built_dll="$consumer_dir/bin/Release/net10.0/$assembly_name.dll"
if [[ ! -f "$built_dll" ]]; then
  echo "error: expected build output not found at $built_dll" >&2
  exit 1
fi

mkdir -p "$artifacts_dir"
cp "$built_dll" "$artifacts_dir/$assembly_name.dll"

if command -v shasum >/dev/null 2>&1; then
  (cd "$artifacts_dir" && shasum -a 256 "$assembly_name.dll" > "$assembly_name.dll.sha256")
else
  (cd "$artifacts_dir" && sha256sum "$assembly_name.dll" > "$assembly_name.dll.sha256")
fi

echo "Baseline artifact rebuilt at $artifacts_dir/$assembly_name.dll"
echo "Update PROVENANCE.md by hand with the commands and versions used above."
