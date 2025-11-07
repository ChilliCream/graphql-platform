#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MODE="${1:-release}"
GATEWAY_PORT=5220
GATEWAY_CPUSET="${GATEWAY_CPUSET:-2-7}"

# Check for taskset availability
HAS_TASKSET=false
if command -v taskset &> /dev/null; then
  HAS_TASKSET=true
else
  echo "⚠ taskset not available - gateway will run without CPU pinning"
fi

# Helper function for optional CPU pinning
maybe_taskset() {
  local cpus="$1"; shift
  if $HAS_TASKSET && [[ -n "${cpus:-}" ]]; then
    taskset -c "$cpus" "$@"
  else
    "$@"
  fi
}

# Helper function to wait for gateway
wait_for_gateway() {
  local max_wait=30
  local count=0

  echo "Waiting for gateway on port $GATEWAY_PORT..."
  while ! curl -s "http://localhost:$GATEWAY_PORT/graphql" -X POST \
    -H "Content-Type: application/json" \
    -d '{"query":"{ __typename }"}' > /dev/null 2>&1; do

    count=$((count + 1))
    if [ $count -ge $max_wait ]; then
      echo "✗ Gateway failed to start after ${max_wait}s"
      return 1
    fi
    sleep 1
  done
  echo "✓ Gateway is ready"
}

echo "============================================"
echo "Starting Gateway ($MODE mode)"
echo "============================================"
echo ""

cd "$SCRIPT_DIR/eShop.Gateway"

# Check if gateway.far exists
if [ ! -f "gateway.far" ]; then
  echo "✗ Error: gateway.far not found!"
  echo "  The gateway configuration file is required."
  exit 1
fi

case "$MODE" in
  aot|AOT)
    echo "Building gateway with AOT compilation..."
    dotnet publish -c Release /p:PublishAot=true --nologo -v quiet -o ./bin/publish-aot

    if [ ! -f "./bin/publish-aot/eShop.Gateway" ]; then
      echo "✗ AOT build failed - executable not found"
      exit 1
    fi

    echo "Starting gateway (AOT) on cores $GATEWAY_CPUSET (port $GATEWAY_PORT)..."
    export ASPNETCORE_ENVIRONMENT=Production
    maybe_taskset "$GATEWAY_CPUSET" ./bin/publish-aot/eShop.Gateway > /dev/null 2>&1 &
    GATEWAY_PID=$!
    ;;

  release|RELEASE|*)
    echo "Building gateway (Release mode)..."
    dotnet build -c Release --nologo -v quiet

    echo "Starting gateway (Release) on cores $GATEWAY_CPUSET (port $GATEWAY_PORT)..."
    export ASPNETCORE_ENVIRONMENT=Production
    maybe_taskset "$GATEWAY_CPUSET" dotnet run -c Release --no-build --no-restore > /dev/null 2>&1 &
    GATEWAY_PID=$!
    ;;
esac

echo "  PID: $GATEWAY_PID"
echo ""

# Wait for gateway to be ready
wait_for_gateway

echo ""
echo "✓ Gateway is running!"
echo ""
echo "  Mode:     $MODE"
echo "  PID:      $GATEWAY_PID"
echo "  Cores:    $GATEWAY_CPUSET"
echo "  Endpoint: http://localhost:$GATEWAY_PORT/graphql"
echo ""
