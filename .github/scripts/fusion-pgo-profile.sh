#!/usr/bin/env bash
# Records the static PGO profile that HotChocolate.Fusion.AspNetCore ships for Native AOT publishes.
#
# Runs the k6 benchmark gateway (JIT, tiered PGO) under deep-recursion load once with the default transport
# configuration and once with alias batching, traces both runs with dotnet-trace, and stops the gateway with SIGTERM
# because the runtime writes its PGO data at shutdown. dotnet-pgo merges both traces into one .mibc file that keeps
# only HotChocolate methods.
#
# dotnet-trace comes from nuget.org. dotnet-pgo is not published on nuget.org, it comes from the public dotnet-eng
# feed of the .NET team. Both are installed into a temporary tool path.
#
# Usage: .github/scripts/fusion-pgo-profile.sh [--output <file>] [--duration <seconds>]
#
# Requires the .NET SDK from global.json, k6, curl, lsof and python3. Ports 5220 to 5224 must be free.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
BENCH_DIR="$REPO_ROOT/src/HotChocolate/Fusion/benchmarks/k6/gf"
GATEWAY_PROJECT="$BENCH_DIR/eShop.Gateway/eShop.Gateway.csproj"
OUTPUT="$REPO_ROOT/src/HotChocolate/Fusion/src/MSBuild/HotChocolate.Fusion.AspNetCore.mibc"
DURATION=60
GATEWAY_PORT=5220
PORTS=(5220 5221 5222 5223 5224)

DOTNET_TRACE_VERSION="10.0.750501"
DOTNET_PGO_VERSION="11.0.0-preview.6.26310.106"
DOTNET_PGO_FEED="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json"
TRACE_PROVIDERS="Microsoft-Windows-DotNETRuntime:0x1F000080018:5"

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
TRACE_PID=""

# Every listener on the benchmark ports was started by this script, the ports were free when it started.
stop_services() {
  local port pids
  for port in "${PORTS[@]}"; do
    pids="$(lsof -ti "tcp:$port" -sTCP:LISTEN 2>/dev/null || true)"
    if [ -n "$pids" ]; then
      kill $pids 2>/dev/null || true
    fi
  done
}

cleanup() {
  local rc=$?
  stop_services
  if [ -n "$TRACE_PID" ]; then
    kill "$TRACE_PID" 2>/dev/null || true
    wait "$TRACE_PID" 2>/dev/null || true
  fi
  if [ "$rc" -eq 0 ]; then
    rm -rf "$WORK_DIR"
  else
    echo "Logs and traces are kept in $WORK_DIR" >&2
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

echo "Installing dotnet-trace $DOTNET_TRACE_VERSION and dotnet-pgo $DOTNET_PGO_VERSION"
TOOLS_DIR="$WORK_DIR/tools"
dotnet tool install dotnet-trace --version "$DOTNET_TRACE_VERSION" --tool-path "$TOOLS_DIR" > /dev/null
dotnet tool install dotnet-pgo --version "$DOTNET_PGO_VERSION" --tool-path "$TOOLS_DIR" \
  --add-source "$DOTNET_PGO_FEED" > /dev/null

echo "Building the gateway"
dotnet build "$GATEWAY_PROJECT" -c Release --nologo -v quiet
GATEWAY_DLL="$(dotnet msbuild "$GATEWAY_PROJECT" -p:Configuration=Release -getProperty:TargetPath)"
GATEWAY_BIN="$(dirname "$GATEWAY_DLL")"

echo "Starting the source schemas"
BENCHMARK_SUBGRAPH_DELAY_MS=4 "$BENCH_DIR/start-source-schemas.sh" > "$WORK_DIR/sources.log" 2>&1

# Writes a copy of the gateway configuration whose source schemas use alias batching only.
python3 -I - "$BENCH_DIR/eShop.Gateway/gateway.far" "$WORK_DIR/gateway-alias.far" <<'PY'
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

# record <name> <gateway.far>: one traced gateway run under k6 load, converted to <name>.mibc.
record() {
  local name=$1 far=$2
  local run_dir="$WORK_DIR/$name" pid
  mkdir -p "$run_dir"
  cp "$far" "$run_dir/gateway.far"
  cp "$BENCH_DIR/eShop.Gateway/appsettings.json" "$run_dir/"

  echo "Recording $name"
  (
    cd "$run_dir"
    exec env ASPNETCORE_URLS="http://localhost:$GATEWAY_PORT" ASPNETCORE_ENVIRONMENT=Production \
      DOTNET_TieredPGO=1 DOTNET_ReadyToRun=0 \
      "$TOOLS_DIR/dotnet-trace" collect --providers "$TRACE_PROVIDERS" --output "$run_dir/trace.nettrace" \
      -- dotnet "$GATEWAY_DLL"
  ) > "$run_dir/trace.log" 2>&1 &
  TRACE_PID=$!
  if ! wait_for_port "$GATEWAY_PORT" "$TRACE_PID"; then
    cat "$run_dir/trace.log" >&2
    exit 1
  fi

  k6 run --quiet -e MODE=constant -e BENCH_OVER_TIME="${DURATION}s" \
    -e GATEWAY_ENDPOINT="http://localhost:$GATEWAY_PORT/graphql" \
    "$BENCH_DIR/deep-recursion.js" > "$run_dir/k6.log" 2>&1

  pid="$(lsof -ti "tcp:$GATEWAY_PORT" -sTCP:LISTEN)"
  kill -TERM "$pid"
  wait "$TRACE_PID" || true
  TRACE_PID=""
  if [ ! -s "$run_dir/trace.nettrace" ]; then
    echo "dotnet-trace wrote no trace for $name, see $run_dir/trace.log" >&2
    cat "$run_dir/trace.log" >&2
    exit 1
  fi

  "$TOOLS_DIR/dotnet-pgo" create-mibc --trace "$run_dir/trace.nettrace" --output "$WORK_DIR/$name.mibc" \
    --include-methods '^HotChocolate\.' --reference "$GATEWAY_BIN/*.dll" > "$run_dir/create-mibc.log"
}

record default "$BENCH_DIR/eShop.Gateway/gateway.far"
record alias-batching "$WORK_DIR/gateway-alias.far"
stop_services

"$TOOLS_DIR/dotnet-pgo" merge --input "$WORK_DIR/default.mibc" --input "$WORK_DIR/alias-batching.mibc" \
  --output "$WORK_DIR/merged.mibc" > /dev/null
"$TOOLS_DIR/dotnet-pgo" dump --input "$WORK_DIR/merged.mibc" --output "$WORK_DIR/merged.json" > /dev/null

python3 -I - "$WORK_DIR/merged.json" <<'PY'
import json, sys
methods = json.load(open(sys.argv[1]))["Methods"]
with_data = sum(1 for m in methods if m.get("InstrumentationData"))
other = sum(1 for m in methods if "]HotChocolate." not in m["Method"])
print(f"{len(methods)} methods, {with_data} with instrumentation data, {other} outside HotChocolate")
if with_data == 0 or other != 0:
    sys.exit("The profile has no instrumentation data or contains methods outside HotChocolate.")
PY

mkdir -p "$(dirname "$OUTPUT")"
cp "$WORK_DIR/merged.mibc" "$OUTPUT"
echo "Wrote $OUTPUT ($(stat -c %s "$OUTPUT") bytes)"
