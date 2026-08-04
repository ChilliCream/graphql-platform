#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESULTS_DIR="$SCRIPT_DIR/results"
K6_CPUSET="${K6_CPUSET:-0-1}"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check for taskset availability
HAS_TASKSET=false
if command -v taskset &> /dev/null; then
  HAS_TASKSET=true
  echo -e "${GREEN}✓${NC} taskset available - CPU pinning enabled"
else
  echo -e "${YELLOW}⚠${NC} taskset not available - running without CPU pinning"
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

# Check for k6
if ! command -v k6 &> /dev/null; then
  echo -e "${YELLOW}k6 is not installed.${NC} Attempting to install..."

  if [[ "$OSTYPE" == "darwin"* ]]; then
    if command -v brew &> /dev/null; then
      brew install k6
    else
      echo -e "${RED}✗${NC} Homebrew not found. Please install k6 manually: https://k6.io/docs/getting-started/installation/"
      exit 1
    fi
  elif command -v apt-get &> /dev/null; then
    sudo gpg -k
    sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
    echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
    sudo apt-get update
    sudo apt-get install k6
  elif command -v yum &> /dev/null; then
    sudo yum install k6
  else
    echo -e "${RED}✗${NC} Unable to auto-install k6. Please install manually: https://k6.io/docs/getting-started/installation/"
    exit 1
  fi
fi

# Check for .NET 9
if ! dotnet --list-sdks | grep -q "^9\."; then
  echo -e "${RED}✗${NC} .NET 9 SDK not found. Please install: https://dotnet.microsoft.com/download/dotnet/9.0"
  exit 1
fi

echo -e "${GREEN}✓${NC} k6 available"
echo -e "${GREEN}✓${NC} .NET 9 SDK available"
echo ""

# Cleanup function
cleanup() {
  echo ""
  echo "Cleaning up..."
  "$SCRIPT_DIR/stop-services.sh"
}

# Register cleanup on exit
trap cleanup EXIT

# Create results directory
mkdir -p "$RESULTS_DIR"

echo "============================================"
echo "HotChocolate Fusion Benchmark Suite"
echo "============================================"
echo ""
echo "Architecture:"
echo "  Cores 0-1:  K6 load generator"
echo "  Cores 2-7:  Gateway"
echo "  Cores 8-15: All source schemas (Accounts, Inventory, Products, Reviews)"
echo ""

# Start source schemas (once, reused for both gateway modes)
echo "============================================"
echo "Step 1: Starting Source Schemas"
echo "============================================"
"$SCRIPT_DIR/start-source-schemas.sh"

# Function to run k6 test
run_k6_test() {
  local test_name=$1
  local test_file=$2
  local gateway_mode=$3
  local output_file="$RESULTS_DIR/results-${gateway_mode}-${test_name}.json"

  echo ""
  echo "Running k6 test: $test_name (gateway: $gateway_mode)"
  echo "  Test file: $test_file"
  echo "  K6 cores:  $K6_CPUSET"
  echo "  Output:    $output_file"
  echo ""

  # Run k6 test (continue even if thresholds fail)
  set +e
  maybe_taskset "$K6_CPUSET" k6 run "$test_file" \
    --out json="$output_file" \
    --summary-export="$RESULTS_DIR/summary-${gateway_mode}-${test_name}.json"
  local exit_code=$?
  set -e

  echo ""
  if [ -f "$RESULTS_DIR/summary-${gateway_mode}-${test_name}.json" ]; then
    if [ $exit_code -eq 0 ]; then
      echo -e "${GREEN}✓${NC} Test completed: $test_name ($gateway_mode)"
    else
      echo -e "${YELLOW}⚠${NC} Test completed with threshold failures: $test_name ($gateway_mode)"
    fi
    echo "  Results saved to: $RESULTS_DIR/summary-${gateway_mode}-${test_name}.json"
  else
    echo -e "${RED}✗${NC} Test failed - results file not found!"
  fi
}

# Function to test gateway mode
test_gateway_mode() {
  local mode=$1

  echo ""
  echo "============================================"
  echo "Testing Gateway: $mode Mode"
  echo "============================================"

  # Start gateway
  "$SCRIPT_DIR/start-gateway.sh" "$mode"

  # Give gateway a moment to fully initialize
  sleep 2

  # Run both k6 tests
  run_k6_test "single" "$SCRIPT_DIR/single-fetch.js" "$mode"
  run_k6_test "no-recursion" "$SCRIPT_DIR/no-recursion.js" "$mode"

  # Stop gateway
  echo ""
  echo "Stopping gateway..."
  lsof -ti:5220 | xargs kill -9 2>/dev/null || true
  sleep 2
}

# Run benchmarks
test_gateway_mode "release"

echo ""
echo "============================================"
echo "Benchmark Complete!"
echo "============================================"
echo ""
echo "Results saved to: $RESULTS_DIR"
echo ""
