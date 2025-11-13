#!/bin/bash

set -eo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
OUTPUT_FILE="${1:-$SCRIPT_DIR/fusion-gateway-performance-data.json}"
NUM_RUNS=3

# Check for taskset availability
HAS_TASKSET=false
if command -v taskset &> /dev/null; then
  HAS_TASKSET=true
  echo "✓ taskset available - CPU pinning enabled"
else
  echo "⚠ taskset not available - k6 will run without CPU pinning"
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

# Cleanup function to stop services on exit
cleanup() {
    echo ""
    echo -e "${YELLOW}Cleaning up - stopping any running services...${NC}"
    "$SCRIPT_DIR/stop-services.sh" > /dev/null 2>&1 || true
}

# Register cleanup function to run on exit
trap cleanup EXIT

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Fusion Gateway k6 Performance Collector${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo "Output file: $OUTPUT_FILE"
echo "Running each test ${NUM_RUNS} times to reduce variance"
if $HAS_TASKSET; then
  echo "CPU Assignments:"
  echo "  k6:                cores 0-1"
  echo "  Gateway:           cores 2-3 (no-recursion, deep-recursion tests)"
  echo "  Source Schemas:    cores 4-15 (no-recursion, deep-recursion tests)"
  echo "  Inventory Service: cores 2-3 (variable-batch test)"
fi
echo ""

# Function to calculate median from multiple values
calculate_median() {
    local values=("$@")
    local count=${#values[@]}
    # Sort the values
    IFS=$'\n' sorted=($(sort -n <<<"${values[*]}"))
    unset IFS
    # Return the middle value (for odd number of values)
    local middle_index=$(( (count - 1) / 2 ))
    echo "${sorted[$middle_index]}"
}

# Function to start infrastructure for no-recursion test
# Gateway on cores 2-3, all source schemas on cores 4-15
start_infrastructure_no_recursion() {
    echo -e "${YELLOW}    Starting source schemas on cores 4-15...${NC}"
    export SOURCES_CPUSET="4-15"
    "$SCRIPT_DIR/start-source-schemas.sh" > /dev/null 2>&1

    echo -e "${YELLOW}    Starting gateway on cores 2-3...${NC}"
    export GATEWAY_CPUSET="2-3"
    "$SCRIPT_DIR/start-gateway.sh" > /dev/null 2>&1

    echo -e "${YELLOW}    Waiting for services to be ready...${NC}"
    sleep 5
}

# Function to start infrastructure for variable-batch test
# Only inventory service on cores 2-3
start_infrastructure_variable_batch() {
    echo -e "${YELLOW}    Starting inventory service on cores 2-3...${NC}"
    export INVENTORY_CPUSET="2-3"
    "$SCRIPT_DIR/start-inventory-only.sh" > /dev/null 2>&1

    echo -e "${YELLOW}    Waiting for service to be ready...${NC}"
    sleep 5
}

# Function to stop infrastructure
stop_infrastructure() {
    echo -e "${YELLOW}    Stopping services...${NC}"
    "$SCRIPT_DIR/stop-services.sh" > /dev/null 2>&1
    sleep 2
}

# Run no-recursion test multiple times
# k6 on cores 0-1, gateway on 2-3, sources on 4-15
echo -e "${BLUE}Running No Recursion Test (${NUM_RUNS} runs)...${NC}"
for i in $(seq 1 $NUM_RUNS); do
    echo -e "${YELLOW}  Run $i/$NUM_RUNS${NC}"
    start_infrastructure_no_recursion
    maybe_taskset "0-1" k6 run --summary-export=/tmp/no-recursion-summary-${i}.json "$SCRIPT_DIR/no-recursion.js"
    stop_infrastructure
done

# Run deep-recursion test multiple times
# k6 on cores 0-1, gateway on 2-3, sources on 4-15
echo -e "${BLUE}Running Deep Recursion Test (${NUM_RUNS} runs)...${NC}"
for i in $(seq 1 $NUM_RUNS); do
    echo -e "${YELLOW}  Run $i/$NUM_RUNS${NC}"
    start_infrastructure_no_recursion
    maybe_taskset "0-1" k6 run --summary-export=/tmp/deep-recursion-summary-${i}.json "$SCRIPT_DIR/deep-recursion.js"
    stop_infrastructure
done

# Run variable-batch-throughput test multiple times
# k6 on cores 0-1, inventory service on 2-3
echo -e "${BLUE}Running Variable Batch Throughput Test (${NUM_RUNS} runs)...${NC}"
for i in $(seq 1 $NUM_RUNS); do
    echo -e "${YELLOW}  Run $i/$NUM_RUNS${NC}"
    start_infrastructure_variable_batch
    maybe_taskset "0-1" k6 run --summary-export=/tmp/variable-batch-summary-${i}.json "$SCRIPT_DIR/variable-batch-throughput.js"
    stop_infrastructure
done

# Parse the summary statistics from k6 JSON output
echo ""
echo -e "${BLUE}Collecting performance metrics...${NC}"

# Function to extract metric from k6 summary JSON
extract_metric() {
    local file=$1
    local metric=$2
    local stat=$3

    # Use jq if available, otherwise use grep/sed
    if command -v jq &> /dev/null; then
        # k6 stores stats directly under the metric, not under .values
        local value=""
        case "$stat" in
            "min")
                value=$(jq -r --arg metric "$metric" '.metrics[$metric].min // null' "$file" 2>/dev/null)
                ;;
            "p(50)")
                # p50 is stored as "med" in k6
                value=$(jq -r --arg metric "$metric" '.metrics[$metric].med // null' "$file" 2>/dev/null)
                ;;
            "max")
                value=$(jq -r --arg metric "$metric" '.metrics[$metric].max // null' "$file" 2>/dev/null)
                ;;
            "avg")
                value=$(jq -r --arg metric "$metric" '.metrics[$metric].avg // null' "$file" 2>/dev/null)
                ;;
            "p(90)")
                value=$(jq -r --arg metric "$metric" '.metrics[$metric]["p(90)"] // null' "$file" 2>/dev/null)
                ;;
            "p(95)")
                value=$(jq -r --arg metric "$metric" '.metrics[$metric]["p(95)"] // null' "$file" 2>/dev/null)
                ;;
            "p(99)")
                value=$(jq -r --arg metric "$metric" '.metrics[$metric]["p(99)"] // null' "$file" 2>/dev/null)
                ;;
            "rate")
                value=$(jq -r --arg metric "$metric" '.metrics[$metric].rate // null' "$file" 2>/dev/null)
                ;;
            "count")
                value=$(jq -r --arg metric "$metric" '.metrics[$metric].count // null' "$file" 2>/dev/null)
                ;;
        esac

        if [ "$value" == "null" ] || [ -z "$value" ]; then
            echo "0"
        else
            echo "$value"
        fi
    else
        grep -A 20 "\"${metric}\"" "$file" | grep "\"${stat}\"" | grep -oE '[0-9]+\.[0-9]+' | head -1 || echo "0"
    fi
}

# Extract metrics from no-recursion test (all runs) and calculate medians
echo -e "${YELLOW}Calculating median values from ${NUM_RUNS} runs...${NC}"

# Arrays to store values from each run
declare -a NO_REC_MIN_VALUES NO_REC_P50_VALUES NO_REC_MAX_VALUES NO_REC_AVG_VALUES
declare -a NO_REC_P90_VALUES NO_REC_P95_VALUES NO_REC_RPS_VALUES
declare -a NO_REC_ERROR_VALUES NO_REC_ITERATIONS_VALUES

# Extract metrics from each run
for i in $(seq 1 $NUM_RUNS); do
    file="/tmp/no-recursion-summary-${i}.json"

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "min")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "min")
    NO_REC_MIN_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(50)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(50)")
    NO_REC_P50_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "max")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "max")
    NO_REC_MAX_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "avg")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "avg")
    NO_REC_AVG_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(90)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(90)")
    NO_REC_P90_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(95)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(95)")
    NO_REC_P95_VALUES+=("$val")

    val=$(extract_metric "$file" "http_reqs{phase:measurement}" "rate")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_reqs" "rate")
    NO_REC_RPS_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_failed{phase:measurement}" "rate")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_failed" "rate")
    NO_REC_ERROR_VALUES+=("$val")

    val=$(extract_metric "$file" "iterations{phase:measurement}" "count")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "iterations" "count")
    NO_REC_ITERATIONS_VALUES+=("$val")
done

# Calculate medians
NO_REC_MIN=$(calculate_median "${NO_REC_MIN_VALUES[@]}")
NO_REC_P50=$(calculate_median "${NO_REC_P50_VALUES[@]}")
NO_REC_MAX=$(calculate_median "${NO_REC_MAX_VALUES[@]}")
NO_REC_AVG=$(calculate_median "${NO_REC_AVG_VALUES[@]}")
NO_REC_P90=$(calculate_median "${NO_REC_P90_VALUES[@]}")
NO_REC_P95=$(calculate_median "${NO_REC_P95_VALUES[@]}")
NO_REC_RPS=$(calculate_median "${NO_REC_RPS_VALUES[@]}")
NO_REC_ERROR_RATE=$(calculate_median "${NO_REC_ERROR_VALUES[@]}")
NO_REC_ITERATIONS=$(calculate_median "${NO_REC_ITERATIONS_VALUES[@]}")

# Extract metrics from deep-recursion test (all runs) and calculate medians
declare -a DEEP_REC_MIN_VALUES DEEP_REC_P50_VALUES DEEP_REC_MAX_VALUES DEEP_REC_AVG_VALUES
declare -a DEEP_REC_P90_VALUES DEEP_REC_P95_VALUES DEEP_REC_RPS_VALUES
declare -a DEEP_REC_ERROR_VALUES DEEP_REC_ITERATIONS_VALUES

# Extract metrics from each run
for i in $(seq 1 $NUM_RUNS); do
    file="/tmp/deep-recursion-summary-${i}.json"

    val=$(extract_metric "$file" "http_req_duration" "min")
    DEEP_REC_MIN_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration" "p(50)")
    DEEP_REC_P50_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration" "max")
    DEEP_REC_MAX_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration" "avg")
    DEEP_REC_AVG_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration" "p(90)")
    DEEP_REC_P90_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration" "p(95)")
    DEEP_REC_P95_VALUES+=("$val")

    val=$(extract_metric "$file" "http_reqs" "rate")
    DEEP_REC_RPS_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_failed" "rate")
    DEEP_REC_ERROR_VALUES+=("$val")

    val=$(extract_metric "$file" "iterations" "count")
    DEEP_REC_ITERATIONS_VALUES+=("$val")
done

# Calculate medians
DEEP_REC_MIN=$(calculate_median "${DEEP_REC_MIN_VALUES[@]}")
DEEP_REC_P50=$(calculate_median "${DEEP_REC_P50_VALUES[@]}")
DEEP_REC_MAX=$(calculate_median "${DEEP_REC_MAX_VALUES[@]}")
DEEP_REC_AVG=$(calculate_median "${DEEP_REC_AVG_VALUES[@]}")
DEEP_REC_P90=$(calculate_median "${DEEP_REC_P90_VALUES[@]}")
DEEP_REC_P95=$(calculate_median "${DEEP_REC_P95_VALUES[@]}")
DEEP_REC_RPS=$(calculate_median "${DEEP_REC_RPS_VALUES[@]}")
DEEP_REC_ERROR_RATE=$(calculate_median "${DEEP_REC_ERROR_VALUES[@]}")
DEEP_REC_ITERATIONS=$(calculate_median "${DEEP_REC_ITERATIONS_VALUES[@]}")

# Extract metrics from variable-batch-throughput test (all runs) and calculate medians
declare -a VAR_BATCH_MIN_VALUES VAR_BATCH_P50_VALUES VAR_BATCH_MAX_VALUES VAR_BATCH_AVG_VALUES
declare -a VAR_BATCH_P90_VALUES VAR_BATCH_P95_VALUES VAR_BATCH_RPS_VALUES
declare -a VAR_BATCH_ERROR_VALUES VAR_BATCH_ITERATIONS_VALUES

# Extract metrics from each run
for i in $(seq 1 $NUM_RUNS); do
    file="/tmp/variable-batch-summary-${i}.json"

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "min")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "min")
    VAR_BATCH_MIN_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(50)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(50)")
    VAR_BATCH_P50_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "max")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "max")
    VAR_BATCH_MAX_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "avg")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "avg")
    VAR_BATCH_AVG_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(90)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(90)")
    VAR_BATCH_P90_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(95)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(95)")
    VAR_BATCH_P95_VALUES+=("$val")

    val=$(extract_metric "$file" "http_reqs{phase:measurement}" "rate")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_reqs" "rate")
    VAR_BATCH_RPS_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_failed{phase:measurement}" "rate")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_failed" "rate")
    VAR_BATCH_ERROR_VALUES+=("$val")

    val=$(extract_metric "$file" "iterations{phase:measurement}" "count")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "iterations" "count")
    VAR_BATCH_ITERATIONS_VALUES+=("$val")
done

# Calculate medians
VAR_BATCH_MIN=$(calculate_median "${VAR_BATCH_MIN_VALUES[@]}")
VAR_BATCH_P50=$(calculate_median "${VAR_BATCH_P50_VALUES[@]}")
VAR_BATCH_MAX=$(calculate_median "${VAR_BATCH_MAX_VALUES[@]}")
VAR_BATCH_AVG=$(calculate_median "${VAR_BATCH_AVG_VALUES[@]}")
VAR_BATCH_P90=$(calculate_median "${VAR_BATCH_P90_VALUES[@]}")
VAR_BATCH_P95=$(calculate_median "${VAR_BATCH_P95_VALUES[@]}")
VAR_BATCH_RPS=$(calculate_median "${VAR_BATCH_RPS_VALUES[@]}")
VAR_BATCH_ERROR_RATE=$(calculate_median "${VAR_BATCH_ERROR_VALUES[@]}")
VAR_BATCH_ITERATIONS=$(calculate_median "${VAR_BATCH_ITERATIONS_VALUES[@]}")

# Create JSON output
cat > "$OUTPUT_FILE" <<EOF
{
  "timestamp": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "num_runs": ${NUM_RUNS},
  "note": "All metrics are median values from ${NUM_RUNS} test runs",
  "tests": {
    "no-recursion": {
      "name": "Simple Composite Query Test",
      "response_time": {
        "min": ${NO_REC_MIN},
        "p50": ${NO_REC_P50},
        "max": ${NO_REC_MAX},
        "avg": ${NO_REC_AVG},
        "p90": ${NO_REC_P90},
        "p95": ${NO_REC_P95}
      },
      "throughput": {
        "requests_per_second": ${NO_REC_RPS},
        "total_iterations": ${NO_REC_ITERATIONS}
      },
      "reliability": {
        "error_rate": ${NO_REC_ERROR_RATE}
      }
    },
    "deep-recursion": {
      "name": "Complex Nested Query Test",
      "response_time": {
        "min": ${DEEP_REC_MIN},
        "p50": ${DEEP_REC_P50},
        "max": ${DEEP_REC_MAX},
        "avg": ${DEEP_REC_AVG},
        "p90": ${DEEP_REC_P90},
        "p95": ${DEEP_REC_P95}
      },
      "throughput": {
        "requests_per_second": ${DEEP_REC_RPS},
        "total_iterations": ${DEEP_REC_ITERATIONS}
      },
      "reliability": {
        "error_rate": ${DEEP_REC_ERROR_RATE}
      }
    },
    "variable-batch-throughput": {
      "name": "Variable Batching Throughput Test",
      "response_time": {
        "min": ${VAR_BATCH_MIN},
        "p50": ${VAR_BATCH_P50},
        "max": ${VAR_BATCH_MAX},
        "avg": ${VAR_BATCH_AVG},
        "p90": ${VAR_BATCH_P90},
        "p95": ${VAR_BATCH_P95}
      },
      "throughput": {
        "requests_per_second": ${VAR_BATCH_RPS},
        "total_iterations": ${VAR_BATCH_ITERATIONS}
      },
      "reliability": {
        "error_rate": ${VAR_BATCH_ERROR_RATE}
      }
    }
  }
}
EOF

echo -e "${GREEN}✓${NC} Performance data written to $OUTPUT_FILE"
cat "$OUTPUT_FILE"

# Clean up temp files
for i in $(seq 1 $NUM_RUNS); do
    rm -f /tmp/no-recursion-summary-${i}.json
    rm -f /tmp/deep-recursion-summary-${i}.json
    rm -f /tmp/variable-batch-summary-${i}.json
done

echo ""
echo -e "${GREEN}Performance test collection complete!${NC}"
echo -e "${YELLOW}Note: All metrics are median values from ${NUM_RUNS} test runs${NC}"
