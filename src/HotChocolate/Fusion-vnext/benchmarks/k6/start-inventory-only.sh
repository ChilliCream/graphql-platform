#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Check for taskset availability
HAS_TASKSET=false
if command -v taskset &> /dev/null; then
  HAS_TASKSET=true
  echo "✓ taskset available - CPU pinning enabled"
else
  echo "⚠ taskset not available - service will run without CPU pinning"
fi

# CPU core assignment for inventory service
INVENTORY_CPUSET="${INVENTORY_CPUSET:-2-3}"

# Helper function for optional CPU pinning
maybe_taskset() {
  local cpus="$1"; shift
  if $HAS_TASKSET && [[ -n "${cpus:-}" ]]; then
    taskset -c "$cpus" "$@"
  else
    "$@"
  fi
}

# Helper function to wait for service
wait_for_service() {
  local name=$1
  local port=$2
  local max_wait=30
  local count=0

  echo "Waiting for $name on port $port..."
  while ! curl -s "http://localhost:$port/graphql" -X POST \
    -H "Content-Type: application/json" \
    -d '{"query":"{ __typename }"}' > /dev/null 2>&1; do

    count=$((count + 1))
    if [ $count -ge $max_wait ]; then
      echo "✗ $name failed to start after ${max_wait}s"
      return 1
    fi
    sleep 1
  done
  echo "✓ $name is ready"
}

echo "============================================"
echo "Starting Inventory Service (Release Mode)"
echo "============================================"
echo ""

# Source Schema: Inventory (Port 5222)
echo "Building eShop.Inventory..."
cd "$SCRIPT_DIR/eShop.Inventory"
dotnet build -c Release --nologo -v quiet
echo "Starting eShop.Inventory on cores $INVENTORY_CPUSET (port 5222)..."
maybe_taskset "$INVENTORY_CPUSET" dotnet run -c Release --no-build --no-restore > /dev/null 2>&1 &
INVENTORY_PID=$!
echo "  PID: $INVENTORY_PID"

# Wait for service to be ready
echo ""
echo "============================================"
echo "Health Check"
echo "============================================"
wait_for_service "Inventory" 5222

echo ""
echo "✓ Inventory service is running!"
echo ""
echo "  PID:   $INVENTORY_PID"
echo "  Cores: $INVENTORY_CPUSET"
echo ""
echo "To stop: ./stop-services.sh"
