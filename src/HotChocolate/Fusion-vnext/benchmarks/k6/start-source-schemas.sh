#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Check for taskset availability
HAS_TASKSET=false
if command -v taskset &> /dev/null; then
  HAS_TASKSET=true
  echo "✓ taskset available - CPU pinning enabled"
else
  echo "⚠ taskset not available - services will run without CPU pinning"
fi

# CPU core assignment - all source schemas share these cores
SOURCES_CPUSET="${SOURCES_CPUSET:-8-15}"

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
echo "Starting Source Schemas (Release Mode)"
echo "============================================"
echo ""

# Source Schema 1: Accounts (Port 5221)
echo "Building eShop.Accounts..."
cd "$SCRIPT_DIR/eShop.Accounts"
dotnet build -c Release --nologo -v quiet
echo "Starting eShop.Accounts on cores $SOURCES_CPUSET (port 5221)..."
maybe_taskset "$SOURCES_CPUSET" dotnet run -c Release --no-build --no-restore > /dev/null 2>&1 &
ACCOUNTS_PID=$!
echo "  PID: $ACCOUNTS_PID"

# Source Schema 2: Inventory (Port 5222)
echo ""
echo "Building eShop.Inventory..."
cd "$SCRIPT_DIR/eShop.Inventory"
dotnet build -c Release --nologo -v quiet
echo "Starting eShop.Inventory on cores $SOURCES_CPUSET (port 5222)..."
maybe_taskset "$SOURCES_CPUSET" dotnet run -c Release --no-build --no-restore > /dev/null 2>&1 &
INVENTORY_PID=$!
echo "  PID: $INVENTORY_PID"

# Source Schema 3: Products (Port 5223)
echo ""
echo "Building eShop.Products..."
cd "$SCRIPT_DIR/eShop.Products"
dotnet build -c Release --nologo -v quiet
echo "Starting eShop.Products on cores $SOURCES_CPUSET (port 5223)..."
maybe_taskset "$SOURCES_CPUSET" dotnet run -c Release --no-build --no-restore > /dev/null 2>&1 &
PRODUCTS_PID=$!
echo "  PID: $PRODUCTS_PID"

# Source Schema 4: Reviews (Port 5224)
echo ""
echo "Building eShop.Reviews..."
cd "$SCRIPT_DIR/eShop.Reviews"
dotnet build -c Release --nologo -v quiet
echo "Starting eShop.Reviews on cores $SOURCES_CPUSET (port 5224)..."
maybe_taskset "$SOURCES_CPUSET" dotnet run -c Release --no-build --no-restore > /dev/null 2>&1 &
REVIEWS_PID=$!
echo "  PID: $REVIEWS_PID"

# Wait for all services to be ready
echo ""
echo "============================================"
echo "Health Checks"
echo "============================================"
wait_for_service "Accounts" 5221
wait_for_service "Inventory" 5222
wait_for_service "Products" 5223
wait_for_service "Reviews" 5224

echo ""
echo "✓ All source schemas are running!"
echo ""
echo "PIDs (all sharing cores $SOURCES_CPUSET):"
echo "  Accounts:  $ACCOUNTS_PID"
echo "  Inventory: $INVENTORY_PID"
echo "  Products:  $PRODUCTS_PID"
echo "  Reviews:   $REVIEWS_PID"
echo ""
echo "To stop: ./stop-services.sh"
