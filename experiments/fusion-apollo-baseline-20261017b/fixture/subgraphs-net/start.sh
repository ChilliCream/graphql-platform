#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

export ASPNETCORE_ENVIRONMENT=Production

# build.sh bundles the SDK this build was made for into the artifact, so the
# subgraphs run on their intended runtime regardless of what the benchmark
# machine has installed. A missing bundle means a broken artifact: falling back
# to a system dotnet would silently benchmark a different runtime, so fail
# instead. This mirrors the Fusion gateway's start.sh.
BUNDLED_DOTNET_DIR="$SCRIPT_DIR/.dotnet"
if [[ ! -d "$BUNDLED_DOTNET_DIR/shared/Microsoft.NETCore.App" ]]; then
  echo "ERROR: no bundled .NET runtime at $BUNDLED_DOTNET_DIR/shared/Microsoft.NETCore.App - run build.sh in $SCRIPT_DIR first" >&2
  exit 1
fi

# The apphost resolves its runtime via DOTNET_ROOT; no `dotnet` CLI (and no
# machine-wide .NET installation) is needed at launch.
export DOTNET_ROOT="$BUNDLED_DOTNET_DIR"

PIDS=()
CSPROJ_FILES=()
while IFS= read -r csproj; do
  [[ -n "$csproj" ]] && CSPROJ_FILES+=("$csproj")
done < <(find "$SCRIPT_DIR" -mindepth 2 -maxdepth 2 -type f -name '*.csproj' | sort)

if [[ ${#CSPROJ_FILES[@]} -eq 0 ]]; then
  echo "Error: no .csproj files found under $SCRIPT_DIR"
  exit 1
fi

# Resolve the framework-dependent apphost `dotnet build` emitted for a project.
# build.sh builds exactly one target framework, so there must be exactly one
# TFM output dir; several means stale output from another target and guessing
# one could launch the wrong runtime.
resolve_apphost() {
  local project_dir="$1"
  local project_name="$2"
  local tfm_dirs=()
  while IFS= read -r tfm_dir; do
    [[ -n "$tfm_dir" ]] && tfm_dirs+=("$tfm_dir")
  done < <(find "$project_dir/bin/Release" -mindepth 1 -maxdepth 1 -type d -name 'net*' 2>/dev/null | sort)
  if [[ ${#tfm_dirs[@]} -eq 0 ]]; then
    echo "ERROR: no build output under $project_dir/bin/Release/net*/ - run build.sh in $SCRIPT_DIR first" >&2
    exit 1
  fi
  if [[ ${#tfm_dirs[@]} -gt 1 ]]; then
    echo "ERROR: ambiguous build output under $project_dir/bin/Release/ - stale output from another target framework:" >&2
    printf '  %s\n' "${tfm_dirs[@]}" >&2
    echo "       re-run build.sh in $SCRIPT_DIR to rebuild a single-target artifact" >&2
    exit 1
  fi
  local apphost="${tfm_dirs[0]}/$project_name"
  if [[ ! -x "$apphost" ]]; then
    echo "ERROR: apphost not found or not executable: $apphost - run build.sh in $SCRIPT_DIR first" >&2
    exit 1
  fi
  printf '%s' "$apphost"
}

# `dotnet run` applies Properties/launchSettings.json, but the apphost does
# not — replicate the http profile's applicationUrl via ASPNETCORE_URLS so
# each subgraph binds the port the gateways expect.
launch_url_for() {
  python3 - "$1/Properties/launchSettings.json" <<'PY'
import json
import sys

with open(sys.argv[1], encoding="utf-8-sig") as fh:
    profiles = json.load(fh).get("profiles", {})
url = profiles.get("http", {}).get("applicationUrl", "")
print(url.split(";")[0].strip())
PY
}

cleanup() {
  for pid in "${PIDS[@]:-}"; do
    kill "$pid" 2>/dev/null || true
  done
}
trap cleanup EXIT

for csproj in "${CSPROJ_FILES[@]}"; do
  project_dir="$(dirname "$csproj")"
  project_name="$(basename "$project_dir")"
  log_file="$(printf '%s' "$project_name" | tr '[:upper:]' '[:lower:]')"
  log_file="${log_file//./_}_log.txt"

  APPHOST="$(resolve_apphost "$project_dir" "$project_name")"
  APP_URL="$(launch_url_for "$project_dir")"
  if [[ -z "$APP_URL" ]]; then
    echo "ERROR: no http applicationUrl in $project_dir/Properties/launchSettings.json" >&2
    exit 1
  fi

  (
    cd "$project_dir"
    export ASPNETCORE_URLS="$APP_URL"
    exec "$APPHOST" > "$SCRIPT_DIR/$log_file" 2>&1
  ) &
  PIDS+=($!)
done

wait
