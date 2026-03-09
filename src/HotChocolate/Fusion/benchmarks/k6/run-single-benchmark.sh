#!/bin/bash

set -eo pipefail

# =============================================================================
# Run a single benchmark test (one test + one mode combination)
# Usage: ./run-single-benchmark.sh <test> <mode> <output-file> [runner-group] [runner-label]
#
# Arguments:
#   test:         no-recursion | deep-recursion | variable-batch
#   mode:         constant | ramping
#   output-file:  Path for the JSON result output
#   runner-group: Name of the runner group (default: unknown)
#   runner-label: Display label for the runner (default: 1)
# =============================================================================

TEST_NAME="${1:?Usage: $0 <test> <mode> <output-file> [runner-group] [runner-label]}"
BENCH_MODE="${2:?Usage: $0 <test> <mode> <output-file> [runner-group] [runner-label]}"
OUTPUT_FILE="${3:?Usage: $0 <test> <mode> <output-file> [runner-group] [runner-label]}"
RUNNER_GROUP="${4:-unknown}"
RUNNER_LABEL="${5:-1}"

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
NUM_RUNS=3

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Check for taskset availability
HAS_TASKSET=false
if command -v taskset &> /dev/null; then
    HAS_TASKSET=true
    echo -e "${GREEN}taskset available - CPU pinning enabled${NC}"
else
    echo -e "${YELLOW}taskset not available - running without CPU pinning${NC}"
fi

maybe_taskset() {
    local cpus="$1"; shift
    if $HAS_TASKSET && [[ -n "${cpus:-}" ]]; then
        taskset -c "$cpus" "$@"
    else
        "$@"
    fi
}

cleanup() {
    echo ""
    echo -e "${YELLOW}Cleaning up - stopping any running services...${NC}"
    "$SCRIPT_DIR/stop-services.sh" > /dev/null 2>&1 || true
}

trap cleanup EXIT

# ---------------------------------------------------------------------------
# CPU pinning — aligned with graphql-gateways-benchmark
# ---------------------------------------------------------------------------
# Constant (50 VUs):  k6: core 0, Gateway: cores 1-2, Sources: 3-(N-1)
# Ramping (500 VUs):  k6: core 0, Gateway: cores 1-3, Sources: 4-(N-1)
#
# Same layout for all runner groups — pinning is by mode, not machine size.
# ---------------------------------------------------------------------------

TOTAL_CORES=$(nproc 2>/dev/null || grep -c ^processor /proc/cpuinfo 2>/dev/null || echo 8)
LAST_CORE=$((TOTAL_CORES - 1))

K6_CPUSET="0"

if [ "$BENCH_MODE" == "constant" ]; then
    GATEWAY_CPUSET_PIN="1-2"
    SOURCES_CPUSET_PIN="3-${LAST_CORE}"
else
    GATEWAY_CPUSET_PIN="1-3"
    SOURCES_CPUSET_PIN="4-${LAST_CORE}"
fi

echo -e "${BLUE}CPU pinning (${BENCH_MODE} mode, ${TOTAL_CORES} cores):${NC}"
echo "  k6:      core ${K6_CPUSET}"
echo "  Gateway: cores ${GATEWAY_CPUSET_PIN}"
echo "  Sources: cores ${SOURCES_CPUSET_PIN}"
echo ""

# ---------------------------------------------------------------------------
# Infrastructure functions
# ---------------------------------------------------------------------------

start_infrastructure_gateway() {
    echo -e "${YELLOW}    Starting source schemas on cores ${SOURCES_CPUSET_PIN}...${NC}"
    export SOURCES_CPUSET="$SOURCES_CPUSET_PIN"
    "$SCRIPT_DIR/start-source-schemas.sh" > /dev/null 2>&1

    echo -e "${YELLOW}    Starting gateway on cores ${GATEWAY_CPUSET_PIN}...${NC}"
    export GATEWAY_CPUSET="$GATEWAY_CPUSET_PIN"
    "$SCRIPT_DIR/start-gateway.sh" > /dev/null 2>&1

    echo -e "${YELLOW}    Waiting for services to be ready...${NC}"
    sleep 5
}

start_infrastructure_variable_batch() {
    echo -e "${YELLOW}    Starting inventory service on cores ${GATEWAY_CPUSET_PIN}...${NC}"
    export INVENTORY_CPUSET="$GATEWAY_CPUSET_PIN"
    "$SCRIPT_DIR/start-inventory-only.sh" > /dev/null 2>&1

    echo -e "${YELLOW}    Waiting for service to be ready...${NC}"
    sleep 5
}

stop_infrastructure() {
    echo -e "${YELLOW}    Stopping services...${NC}"
    "$SCRIPT_DIR/stop-services.sh" > /dev/null 2>&1
    sleep 2
}

# ---------------------------------------------------------------------------
# Metric extraction from k6 summary JSON
# ---------------------------------------------------------------------------

extract_metric() {
    local file=$1
    local metric=$2
    local stat=$3
    local value=""

    case "$stat" in
        "min")   value=$(jq -r --arg m "$metric" '.metrics[$m].min // null' "$file" 2>/dev/null) ;;
        "p(50)") value=$(jq -r --arg m "$metric" '.metrics[$m].med // null' "$file" 2>/dev/null) ;;
        "max")   value=$(jq -r --arg m "$metric" '.metrics[$m].max // null' "$file" 2>/dev/null) ;;
        "avg")   value=$(jq -r --arg m "$metric" '.metrics[$m].avg // null' "$file" 2>/dev/null) ;;
        "p(90)") value=$(jq -r --arg m "$metric" '.metrics[$m]["p(90)"] // null' "$file" 2>/dev/null) ;;
        "p(95)") value=$(jq -r --arg m "$metric" '.metrics[$m]["p(95)"] // null' "$file" 2>/dev/null) ;;
        "rate")  value=$(jq -r --arg m "$metric" '.metrics[$m].rate // null' "$file" 2>/dev/null) ;;
        "count") value=$(jq -r --arg m "$metric" '.metrics[$m].count // null' "$file" 2>/dev/null) ;;
    esac

    if [ "$value" == "null" ] || [ -z "$value" ]; then
        echo "0"
    else
        echo "$value"
    fi
}

calculate_median() {
    local values=("$@")
    local count=${#values[@]}
    IFS=$'\n' sorted=($(sort -n <<<"${values[*]}"))
    unset IFS
    local middle_index=$(( (count - 1) / 2 ))
    echo "${sorted[$middle_index]}"
}

# Extract a metric with optional fallback to untagged metric
extract_with_fallback() {
    local file=$1
    local primary=$2
    local fallback=$3
    local stat=$4

    local val
    val=$(extract_metric "$file" "$primary" "$stat")
    if [ "$val" == "0" ] && [ -n "$fallback" ]; then
        val=$(extract_metric "$file" "$fallback" "$stat")
    fi
    echo "$val"
}

# ---------------------------------------------------------------------------
# Determine k6 script and metric keys
# ---------------------------------------------------------------------------

case "$TEST_NAME" in
    no-recursion)   K6_SCRIPT="$SCRIPT_DIR/no-recursion.js" ;;
    deep-recursion) K6_SCRIPT="$SCRIPT_DIR/deep-recursion.js" ;;
    variable-batch) K6_SCRIPT="$SCRIPT_DIR/variable-batch-throughput.js" ;;
    *) echo "Unknown test: $TEST_NAME"; exit 1 ;;
esac

# no-recursion and variable-batch use phase-tagged metrics in constant mode
if [[ "$BENCH_MODE" == "constant" && ("$TEST_NAME" == "no-recursion" || "$TEST_NAME" == "variable-batch") ]]; then
    DURATION_METRIC="http_req_duration{phase:measurement}"
    REQS_METRIC="http_reqs{phase:measurement}"
    FAILED_METRIC="http_req_failed{phase:measurement}"
    ITER_METRIC="iterations{phase:measurement}"
    DURATION_FALLBACK="http_req_duration"
    REQS_FALLBACK="http_reqs"
    FAILED_FALLBACK="http_req_failed"
    ITER_FALLBACK="iterations"
else
    DURATION_METRIC="http_req_duration"
    REQS_METRIC="http_reqs"
    FAILED_METRIC="http_req_failed"
    ITER_METRIC="iterations"
    DURATION_FALLBACK=""
    REQS_FALLBACK=""
    FAILED_FALLBACK=""
    ITER_FALLBACK=""
fi

# ---------------------------------------------------------------------------
# Start infrastructure based on test + mode
# ---------------------------------------------------------------------------

start_infra() {
    case "$TEST_NAME" in
        no-recursion|deep-recursion)
            start_infrastructure_gateway
            ;;
        variable-batch)
            start_infrastructure_variable_batch
            ;;
    esac
}

# Run k6 with the correct MODE env var and runner-aware CPU pinning
run_k6() {
    local summary_file=$1
    if [ "$BENCH_MODE" == "ramping" ]; then
        MODE=ramping maybe_taskset "$K6_CPUSET" k6 run --summary-export="$summary_file" "$K6_SCRIPT"
    else
        maybe_taskset "$K6_CPUSET" k6 run --summary-export="$summary_file" "$K6_SCRIPT"
    fi
}

# Extract all metrics from a k6 summary file into RESULT_* variables
extract_results_from_file() {
    local file=$1

    RESULT_MIN=$(extract_with_fallback "$file" "$DURATION_METRIC" "$DURATION_FALLBACK" "min")
    RESULT_P50=$(extract_with_fallback "$file" "$DURATION_METRIC" "$DURATION_FALLBACK" "p(50)")
    RESULT_MAX=$(extract_with_fallback "$file" "$DURATION_METRIC" "$DURATION_FALLBACK" "max")
    RESULT_AVG=$(extract_with_fallback "$file" "$DURATION_METRIC" "$DURATION_FALLBACK" "avg")
    RESULT_P90=$(extract_with_fallback "$file" "$DURATION_METRIC" "$DURATION_FALLBACK" "p(90)")
    RESULT_P95=$(extract_with_fallback "$file" "$DURATION_METRIC" "$DURATION_FALLBACK" "p(95)")
    RESULT_RPS=$(extract_with_fallback "$file" "$REQS_METRIC" "$REQS_FALLBACK" "rate")
    RESULT_ERR=$(extract_with_fallback "$file" "$FAILED_METRIC" "$FAILED_FALLBACK" "rate")
    RESULT_ITER=$(extract_with_fallback "$file" "$ITER_METRIC" "$ITER_FALLBACK" "count")
}

# ---------------------------------------------------------------------------
# Run the benchmark
# ---------------------------------------------------------------------------

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Benchmark: ${TEST_NAME} (${BENCH_MODE})${NC}"
echo -e "${BLUE}Runner:    ${RUNNER_GROUP} (${RUNNER_LABEL})${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

if [ "$BENCH_MODE" == "constant" ]; then
    echo -e "${BLUE}Running ${NUM_RUNS} iterations for median calculation...${NC}"

    declare -a MIN_V P50_V MAX_V AVG_V P90_V P95_V RPS_V ERR_V ITER_V

    for i in $(seq 1 $NUM_RUNS); do
        echo -e "${YELLOW}  Run $i/$NUM_RUNS${NC}"
        start_infra
        run_k6 "/tmp/benchmark-summary-${i}.json"
        stop_infrastructure

        extract_results_from_file "/tmp/benchmark-summary-${i}.json"
        MIN_V+=("$RESULT_MIN")
        P50_V+=("$RESULT_P50")
        MAX_V+=("$RESULT_MAX")
        AVG_V+=("$RESULT_AVG")
        P90_V+=("$RESULT_P90")
        P95_V+=("$RESULT_P95")
        RPS_V+=("$RESULT_RPS")
        ERR_V+=("$RESULT_ERR")
        ITER_V+=("$RESULT_ITER")
    done

    RESULT_MIN=$(calculate_median "${MIN_V[@]}")
    RESULT_P50=$(calculate_median "${P50_V[@]}")
    RESULT_MAX=$(calculate_median "${MAX_V[@]}")
    RESULT_AVG=$(calculate_median "${AVG_V[@]}")
    RESULT_P90=$(calculate_median "${P90_V[@]}")
    RESULT_P95=$(calculate_median "${P95_V[@]}")
    RESULT_RPS=$(calculate_median "${RPS_V[@]}")
    RESULT_ERR=$(calculate_median "${ERR_V[@]}")
    RESULT_ITER=$(calculate_median "${ITER_V[@]}")
else
    echo -e "${BLUE}Running single ramping iteration...${NC}"
    start_infra
    run_k6 "/tmp/benchmark-summary.json"
    stop_infrastructure

    extract_results_from_file "/tmp/benchmark-summary.json"
fi

# ---------------------------------------------------------------------------
# Write JSON output
# ---------------------------------------------------------------------------

cat > "$OUTPUT_FILE" <<EOF
{
  "timestamp": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "runner_group": "${RUNNER_GROUP}",
  "runner_label": "${RUNNER_LABEL}",
  "test": "${TEST_NAME}",
  "mode": "${BENCH_MODE}",
  "num_runs": $([ "$BENCH_MODE" == "constant" ] && echo "$NUM_RUNS" || echo "1"),
  "response_time": {
    "min": ${RESULT_MIN},
    "p50": ${RESULT_P50},
    "max": ${RESULT_MAX},
    "avg": ${RESULT_AVG},
    "p90": ${RESULT_P90},
    "p95": ${RESULT_P95}
  },
  "throughput": {
    "requests_per_second": ${RESULT_RPS},
    "total_iterations": ${RESULT_ITER}
  },
  "reliability": {
    "error_rate": ${RESULT_ERR}
  }
}
EOF

echo ""
echo -e "${GREEN}Result written to $OUTPUT_FILE${NC}"
cat "$OUTPUT_FILE"

# Clean up temp files
for i in $(seq 1 $NUM_RUNS); do
    rm -f "/tmp/benchmark-summary-${i}.json"
done
rm -f /tmp/benchmark-summary.json

echo ""
echo -e "${GREEN}Benchmark complete!${NC}"
