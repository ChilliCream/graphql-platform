#!/usr/bin/env bash
# Records the code order file that HotChocolate.Fusion.AspNetCore ships for Native AOT publishes.
#
# Publishes the k6 benchmark gateways for composite schemas and for Apollo Federation as Native AOT with the static PGO
# profile, and runs them under fusion-code-order-recorder.py with their source schemas: the composite gateway with the
# default transport configuration and with alias batching, then the Apollo Federation gateway. Every run is put under
# deep-recursion load. The first run also stays idle for more than a minute and is put under load again, so that the
# periodic work of the gateway runs. The file lists the names of the executed methods in the order of their first
# execution, a method that several runs execute once. The methods that the file in the repository lists and no run
# executed follow them.
#
# Usage: .github/scripts/fusion-code-order.sh [--output <file>] [--duration <seconds>]
#
# Requires Linux on x86-64, the .NET SDK from global.json with the Native AOT prerequisites, k6, curl, lsof and
# python3. Ports 5220 to 5224 must be free.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
K6_DIR="$REPO_ROOT/src/HotChocolate/Fusion/benchmarks/k6"
MSBUILD_DIR="$REPO_ROOT/src/HotChocolate/Fusion/src/MSBuild"
RECORDER="$REPO_ROOT/.github/scripts/fusion-code-order-recorder.py"
REPOSITORY_FILE="$MSBUILD_DIR/HotChocolate.Fusion.AspNetCore.order"
OUTPUT="$REPOSITORY_FILE"
DURATION=30
IDLE_SECONDS=70
GATEWAY_PORT=5220
PORTS=(5220 5221 5222 5223 5224)

usage() {
  echo "Usage: $0 [--output <file>] [--duration <seconds>]" >&2
  exit 2
}

while [ $# -gt 0 ]; do
  case "$1" in
    --output) OUTPUT="${2:?--output needs a file}"; shift 2 ;;
    --duration) DURATION="${2:?--duration needs seconds}"; shift 2 ;;
    -h|--help) usage ;;
    *) echo "Unknown argument: $1" >&2; usage ;;
  esac
done

if [ "$(uname -s)" != "Linux" ] || [ "$(uname -m)" != "x86_64" ]; then
  echo "The code order file is recorded on Linux on x86-64 only." >&2
  exit 1
fi

for tool in dotnet k6 curl lsof python3; do
  command -v "$tool" > /dev/null || { echo "$tool is required" >&2; exit 1; }
done

for port in "${PORTS[@]}"; do
  if lsof -ti "tcp:$port" -sTCP:LISTEN > /dev/null 2>&1; then
    echo "Port $port is in use. Stop the process that listens on it and run the script again." >&2
    exit 1
  fi
done

WORK_DIR="$(mktemp -d)"
RECORDER_PID=""

# listeners: prints the ids of the processes that listen on the benchmark ports.
listeners() {
  local port
  for port in "${PORTS[@]}"; do
    lsof -ti "tcp:$port" -sTCP:LISTEN 2>/dev/null || true
  done
}

# Every listener on the benchmark ports was started by this script, the ports were free when it started.
stop_services() {
  local pids i
  pids="$(listeners)"
  if [ -n "$pids" ]; then
    kill $pids 2>/dev/null || true
  fi
  for i in $(seq 1 30); do
    if [ -z "$(listeners)" ]; then
      return 0
    fi
    sleep 1
  done
  echo "A benchmark port is still in use" >&2
  return 1
}

cleanup() {
  local rc=$?
  if [ -n "$RECORDER_PID" ]; then
    kill "$RECORDER_PID" 2>/dev/null || true
    wait "$RECORDER_PID" 2>/dev/null || true
  fi
  stop_services || true
  if [ "$rc" -eq 0 ]; then
    rm -rf "$WORK_DIR"
  else
    echo "Logs and recordings are kept in $WORK_DIR" >&2
  fi
}
trap cleanup EXIT
trap 'exit 143' INT TERM

# wait_for_port <port> [pid]: waits until a GraphQL server answers on <port>, failing early when <pid> exits.
wait_for_port() {
  local port=$1 pid=${2:-} i
  for i in $(seq 1 60); do
    if curl -s "http://localhost:$port/graphql" -X POST -H "Content-Type: application/json" \
        -d '{"query":"{ __typename }"}' > /dev/null 2>&1; then
      return 0
    fi
    if [ -n "$pid" ] && ! kill -0 "$pid" 2>/dev/null; then
      break
    fi
    sleep 1
  done
  echo "Nothing answered on port $port" >&2
  return 1
}

# publish <flavor>: publishes the gateway of the benchmark in $K6_DIR/<flavor> as Native AOT to
# $WORK_DIR/publish-<flavor>, compiled like a gateway that references the package, without a code order file. The
# projects that the gateway references are built for its target framework only.
publish() {
  local flavor=$1 project="$K6_DIR/$1/eShop.Gateway/eShop.Gateway.csproj" framework
  framework="$(dotnet msbuild "$project" -getProperty:TargetFramework)"
  echo "Publishing the $flavor gateway for $framework"
  dotnet publish "$project" -c Release --nologo -v quiet -f "$framework" -p:TargetFrameworks="$framework" \
    -o "$WORK_DIR/publish-$flavor" -p:PublishAot=true -p:HotChocolateFusionCodeOrder=disable \
    -p:CustomAfterMicrosoftCommonTargets="$MSBUILD_DIR/HotChocolate.Fusion.AspNetCore.targets"
  if [ ! -x "$WORK_DIR/publish-$flavor/eShop.Gateway" ] || [ ! -s "$WORK_DIR/publish-$flavor/eShop.Gateway.dbg" ]; then
    echo "The $flavor gateway was not published with its debug file." >&2
    exit 1
  fi
}

# load <run directory> <k6 script> <seconds>: puts the gateway under constant load.
load() {
  k6 run --quiet -e MODE=constant -e BENCH_OVER_TIME="${3}s" \
    -e GATEWAY_ENDPOINT="http://localhost:$GATEWAY_PORT/graphql" "$2" >> "$1/k6.log" 2>&1
}

# record <name> <flavor> <gateway.far> <idle seconds>: one recorded run of the gateway of <flavor> under the load of
# its deep-recursion script, written to <name>.order.
record() {
  local name=$1 flavor=$2 far=$3 idle=$4
  local run_dir="$WORK_DIR/$name" program="$WORK_DIR/publish-$flavor/eShop.Gateway"
  local script="$K6_DIR/$flavor/deep-recursion.js"
  mkdir -p "$run_dir"
  cp "$far" "$run_dir/gateway.far"
  cp "$K6_DIR/$flavor/eShop.Gateway/appsettings.json" "$run_dir/"

  echo "Recording $name"
  (
    cd "$run_dir"
    exec env ASPNETCORE_URLS="http://localhost:$GATEWAY_PORT" ASPNETCORE_ENVIRONMENT=Production \
      python3 -I "$RECORDER" --symbols "$program.dbg" --output "$WORK_DIR/$name.order" -- "$program"
  ) > "$run_dir/gateway.log" 2>&1 &
  RECORDER_PID=$!
  if ! wait_for_port "$GATEWAY_PORT" "$RECORDER_PID"; then
    cat "$run_dir/gateway.log" >&2
    exit 1
  fi

  load "$run_dir" "$script" "$DURATION"
  if [ "$idle" -gt 0 ]; then
    sleep "$idle"
    load "$run_dir" "$script" 10
  fi

  kill -TERM "$RECORDER_PID"
  wait "$RECORDER_PID" || true
  RECORDER_PID=""
  tail -n 1 "$run_dir/gateway.log"
  if [ ! -s "$WORK_DIR/$name.order" ]; then
    echo "The recorder wrote no methods for $name, see $run_dir/gateway.log" >&2
    cat "$run_dir/gateway.log" >&2
    exit 1
  fi
}

publish gf
publish af

# Writes a copy of the gateway configuration whose source schemas use alias batching only.
python3 -I - "$K6_DIR/gf/eShop.Gateway/gateway.far" "$WORK_DIR/gateway-alias.far" <<'PY'
import json, sys, zipfile
with zipfile.ZipFile(sys.argv[1]) as source, zipfile.ZipFile(sys.argv[2], "w", zipfile.ZIP_DEFLATED) as target:
    for entry in source.infolist():
        data = source.read(entry.filename)
        if entry.filename.endswith("gateway-settings.json"):
            settings = json.loads(data)
            for schema in settings["sourceSchemas"].values():
                schema["transports"]["http"]["capabilities"] = {
                    "batching": {"variableBatching": False, "requestBatching": False, "aliasBatching": True}
                }
            data = json.dumps(settings, indent=2).encode()
        target.writestr(entry, data)
PY

echo "Starting the composite source schemas"
"$K6_DIR/gf/start-source-schemas.sh" > "$WORK_DIR/sources-composite.log" 2>&1
record composite gf "$K6_DIR/gf/eShop.Gateway/gateway.far" "$IDLE_SECONDS"
record composite-alias-batching gf "$WORK_DIR/gateway-alias.far" 0
stop_services

echo "Starting the Apollo Federation source schemas"
"$K6_DIR/af/start-source-schemas.sh" > "$WORK_DIR/sources-apollo-federation.log" 2>&1
record apollo-federation af "$K6_DIR/af/eShop.Gateway/gateway.far" 0
stop_services

python3 -I - "$WORK_DIR/merged.order" "$REPOSITORY_FILE" "$WORK_DIR/composite.order" \
  "$WORK_DIR/composite-alias-batching.order" "$WORK_DIR/apollo-federation.order" <<'PY'
import os, sys


def read(path, names):
    with open(path) as file:
        for line in file:
            name = line.strip()
            # The methods of the benchmark gateways themselves are not part of a gateway that references the package.
            if name and not name.startswith("eShop_Gateway"):
                names.setdefault(name, None)


names = {}
for path in sys.argv[3:]:
    read(path, names)
recorded = len(names)
fusion = sum(1 for name in names if name.startswith("HotChocolate_Fusion"))
if recorded < 1000 or fusion == 0 or any(" " in name for name in names):
    sys.exit("The recording is too small, has no Fusion methods or contains a name with a space.")
if os.path.exists(sys.argv[2]):
    read(sys.argv[2], names)
print(f"{recorded} methods recorded, {fusion} of them from HotChocolate.Fusion assemblies, "
      f"{len(names) - recorded} more from the file in the repository")
with open(sys.argv[1], "w") as merged:
    merged.write("".join(name + "\n" for name in names))
PY

mkdir -p "$(dirname "$OUTPUT")"
cp "$WORK_DIR/merged.order" "$OUTPUT"
echo "Wrote $OUTPUT ($(wc -l < "$OUTPUT") methods)"
