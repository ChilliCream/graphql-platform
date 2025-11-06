#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RESULTS_DIR="$SCRIPT_DIR/results"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Check if jq is available
HAS_JQ=false
if command -v jq &> /dev/null; then
  HAS_JQ=true
fi

# Extract metric from k6 summary JSON
get_metric() {
  local file=$1
  local metric=$2

  if [ ! -f "$file" ]; then
    echo "N/A"
    return
  fi

  if $HAS_JQ; then
    jq -r ".metrics.${metric}.values.\"p(50)\" // .metrics.${metric}.values.avg // \"N/A\"" "$file" 2>/dev/null || echo "N/A"
  else
    # Fallback to grep/sed
    grep -A 20 "\"$metric\"" "$file" 2>/dev/null | grep -m 1 "\"p(50)\"" | sed 's/.*: \([0-9.]*\).*/\1/' || echo "N/A"
  fi
}

# Calculate percentage difference
calc_percent() {
  local aot=$1
  local release=$2

  if [[ "$aot" == "N/A" || "$release" == "N/A" ]]; then
    echo "N/A"
    return
  fi

  # Calculate percentage improvement (negative = regression)
  local diff=$(echo "scale=2; (($release - $aot) / $release) * 100" | bc -l)
  echo "$diff"
}

# Format percentage with color
format_percent() {
  local value=$1

  if [[ "$value" == "N/A" ]]; then
    echo "N/A"
    return
  fi

  local num=$(echo "$value" | awk '{print ($1 > 0) ? $1 : -$1}')

  if (( $(echo "$value > 5" | bc -l) )); then
    echo -e "${GREEN}+${num}%${NC}"
  elif (( $(echo "$value < -5" | bc -l) )); then
    echo -e "${RED}-${num}%${NC}"
  else
    echo "${value}%"
  fi
}

# Check for results
if [ ! -d "$RESULTS_DIR" ]; then
  echo "Error: Results directory not found. Run ./run-benchmarks.sh first."
  exit 1
fi

echo "============================================"
echo "HotChocolate Fusion: AOT vs Release"
echo "============================================"
echo ""

# Helper function to extract and display metrics
compare_test() {
  local test_name=$1
  local display_name=$2

  local aot_file="$RESULTS_DIR/summary-aot-${test_name}.json"
  local release_file="$RESULTS_DIR/summary-release-${test_name}.json"

  if [ ! -f "$aot_file" ] || [ ! -f "$release_file" ]; then
    echo "⚠ Results missing for $display_name test"
    return
  fi

  echo "Test: $display_name"
  echo "────────────────────────────────────────────"

  # Extract metrics
  local aot_p50=$(get_metric "$aot_file" "http_req_duration")
  local release_p50=$(get_metric "$release_file" "http_req_duration")
  local aot_p95=$(get_metric "$aot_file" "http_req_duration" | jq -r '.values."p(95)"' 2>/dev/null || echo "N/A")
  local release_p95=$(get_metric "$release_file" "http_req_duration" | jq -r '.values."p(95)"' 2>/dev/null || echo "N/A")
  local aot_p99=$(get_metric "$aot_file" "http_req_duration" | jq -r '.values."p(99)"' 2>/dev/null || echo "N/A")
  local release_p99=$(get_metric "$release_file" "http_req_duration" | jq -r '.values."p(99)"' 2>/dev/null || echo "N/A")

  # Get RPS (requests per second)
  local aot_rps="N/A"
  local release_rps="N/A"
  if $HAS_JQ; then
    aot_rps=$(jq -r '.metrics.http_reqs.values.rate // "N/A"' "$aot_file" 2>/dev/null)
    release_rps=$(jq -r '.metrics.http_reqs.values.rate // "N/A"' "$release_file" 2>/dev/null)
  fi

  # Format output
  printf "%-20s %-15s %-15s %-10s\n" "Metric" "AOT" "Release" "Δ"
  printf "%-20s %-15s %-15s %-10s\n" "────────────────────" "─────────────" "─────────────" "────────"

  if $HAS_JQ && [ -f "$aot_file" ] && [ -f "$release_file" ]; then
    # Get actual values with jq
    aot_p50=$(jq -r '.metrics.http_req_duration.values."p(50)" // "N/A"' "$aot_file")
    release_p50=$(jq -r '.metrics.http_req_duration.values."p(50)" // "N/A"' "$release_file")
    aot_p95=$(jq -r '.metrics.http_req_duration.values."p(95)" // "N/A"' "$aot_file")
    release_p95=$(jq -r '.metrics.http_req_duration.values."p(95)" // "N/A"' "$release_file")
    aot_p99=$(jq -r '.metrics.http_req_duration.values."p(99)" // "N/A"' "$aot_file")
    release_p99=$(jq -r '.metrics.http_req_duration.values."p(99)" // "N/A"' "$release_file")

    local p50_diff=$(calc_percent "$aot_p50" "$release_p50")
    local p95_diff=$(calc_percent "$aot_p95" "$release_p95")
    local p99_diff=$(calc_percent "$aot_p99" "$release_p99")
    local rps_diff=$(calc_percent "$release_rps" "$aot_rps")  # Inverted: higher RPS is better

    printf "%-20s %-15.2f %-15.2f %s\n" "p50 latency (ms)" "$aot_p50" "$release_p50" "$(format_percent "$p50_diff")"
    printf "%-20s %-15.2f %-15.2f %s\n" "p95 latency (ms)" "$aot_p95" "$release_p95" "$(format_percent "$p95_diff")"
    printf "%-20s %-15.2f %-15.2f %s\n" "p99 latency (ms)" "$aot_p99" "$release_p99" "$(format_percent "$p99_diff")"
    printf "%-20s %-15.2f %-15.2f %s\n" "Throughput (RPS)" "$aot_rps" "$release_rps" "$(format_percent "$rps_diff")"
  else
    echo "⚠ jq not available - install jq for detailed metrics"
    echo "  AOT results:     $(basename "$aot_file")"
    echo "  Release results: $(basename "$release_file")"
  fi

  echo ""
}

# Compare both tests
compare_test "single" "Single Fetch (Top Product)"
compare_test "stress" "Federation Stress (Complex Query)"

echo "────────────────────────────────────────────"
echo ""
echo "Results location: $RESULTS_DIR"
echo ""
echo "Legend:"
echo -e "  ${GREEN}Green${NC}  = AOT improvement (>5%)"
echo -e "  ${RED}Red${NC}    = AOT regression (<-5%)"
echo "  Yellow = Neutral (-5% to +5%)"
echo ""

if ! $HAS_JQ; then
  echo "💡 Tip: Install jq for better metric parsing"
  echo "   macOS: brew install jq"
  echo "   Linux: apt-get install jq"
  echo ""
fi
