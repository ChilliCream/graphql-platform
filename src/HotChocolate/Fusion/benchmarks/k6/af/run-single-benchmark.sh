#!/bin/bash

set -eo pipefail

# =============================================================================
# Run a single benchmark test (one test + one mode combination)
# Usage: ./run-single-benchmark.sh <test> <mode> <output-file> [runner-group] [runner-label]
#
# Arguments:
#   test:         no-recursion | deep-recursion
#   mode:         constant | constant-latency | ramping (alias: burst)
#   output-file:  Path for the JSON result output
#   runner-group: Name of the runner group (default: unknown)
#   runner-label: Display label for the runner (default: 1)
#
# Environment variables:
#   BENCH_RUNS                   Measured runs, the median is reported (default: 3)
#   MEASURE_SECONDS              Duration of each run (default: 120, ramping: 60)
#   BENCH_VUS                    Virtual users (default: 50, ramping: 500)
#   BENCHMARK_SUBGRAPH_DELAY_MS  Delay per source schema request (default: 4 in constant-latency, else 0)
#
# Methodology (mirrors k6/benchmark.sh of graphql-federation-benchmarks):
#   The services start once, a full-duration warmup run is discarded, then
#   BENCH_RUNS measured runs follow against the same services.
# =============================================================================

TEST_NAME="${1:?Usage: $0 <test> <mode> <output-file> [runner-group] [runner-label]}"
BENCH_MODE="${2:?Usage: $0 <test> <mode> <output-file> [runner-group] [runner-label]}"
OUTPUT_FILE="${3:?Usage: $0 <test> <mode> <output-file> [runner-group] [runner-label]}"
RUNNER_GROUP="${4:-unknown}"
RUNNER_LABEL="${5:-1}"

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
NUM_RUNS="${BENCH_RUNS:-3}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

case "$BENCH_MODE" in
    constant)
        K6_MODE="constant"
        export BENCHMARK_SUBGRAPH_DELAY_MS="${BENCHMARK_SUBGRAPH_DELAY_MS:-0}"
        ;;
    constant-latency)
        # Same k6 scenario as "constant", but the source schemas simulate IO latency.
        K6_MODE="constant"
        export BENCHMARK_SUBGRAPH_DELAY_MS="${BENCHMARK_SUBGRAPH_DELAY_MS:-4}"
        ;;
    ramping|burst)
        BENCH_MODE="ramping"
        K6_MODE="ramping"
        export BENCHMARK_SUBGRAPH_DELAY_MS="${BENCHMARK_SUBGRAPH_DELAY_MS:-0}"
        ;;
    *)
        echo -e "${RED}Unknown mode: $BENCH_MODE (expected constant, constant-latency or ramping)${NC}"
        exit 1
        ;;
esac

if [ "$K6_MODE" == "ramping" ]; then
    MEASURE_SECONDS="${MEASURE_SECONDS:-60}"
else
    MEASURE_SECONDS="${MEASURE_SECONDS:-120}"
fi

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
# CPU pinning, aligned with profiles.json of graphql-federation-benchmarks
# ---------------------------------------------------------------------------
# 16 cores:   k6: 1,9   Gateway: 2-3,10-11   Sources: 4-7,12-15   (SMT siblings stay together)
# 8+ cores:   k6: 1     Gateway: 2-3         Sources: 4-7         (constant)
#             k6: 1     Gateway: 2-4         Sources: 5-7         (ramping)
# Core 0 (and 8 on 16 cores) stays free for the system.
# Fewer than 8 cores run without pinning.
# ---------------------------------------------------------------------------

TOTAL_CORES=$(nproc 2>/dev/null || grep -c ^processor /proc/cpuinfo 2>/dev/null || echo 8)

if [ "$TOTAL_CORES" -eq 16 ]; then
    K6_CPUSET="1,9"
    GATEWAY_CPUSET_PIN="2-3,10-11"
    SOURCES_CPUSET_PIN="4-7,12-15"
elif [ "$TOTAL_CORES" -ge 8 ]; then
    K6_CPUSET="1"
    if [ "$K6_MODE" == "ramping" ]; then
        GATEWAY_CPUSET_PIN="2-4"
        SOURCES_CPUSET_PIN="5-7"
    else
        GATEWAY_CPUSET_PIN="2-3"
        SOURCES_CPUSET_PIN="4-7"
    fi
else
    K6_CPUSET=""
    GATEWAY_CPUSET_PIN=""
    SOURCES_CPUSET_PIN=""
fi

echo -e "${BLUE}CPU pinning (${BENCH_MODE} mode, ${TOTAL_CORES} cores):${NC}"
echo "  k6:      ${K6_CPUSET:-unpinned}"
echo "  Gateway: ${GATEWAY_CPUSET_PIN:-unpinned}"
echo "  Sources: ${SOURCES_CPUSET_PIN:-unpinned}"
echo "  Source schema delay: ${BENCHMARK_SUBGRAPH_DELAY_MS}ms"
echo ""

# ---------------------------------------------------------------------------
# Infrastructure functions
# ---------------------------------------------------------------------------

start_infrastructure_gateway() {
    echo -e "${YELLOW}    Starting source schemas on cores ${SOURCES_CPUSET_PIN:-unpinned}...${NC}"
    export SOURCES_CPUSET="$SOURCES_CPUSET_PIN"
    "$SCRIPT_DIR/start-source-schemas.sh" > /dev/null 2>&1

    echo -e "${YELLOW}    Starting gateway on cores ${GATEWAY_CPUSET_PIN:-unpinned}...${NC}"
    export GATEWAY_CPUSET="$GATEWAY_CPUSET_PIN"
    "$SCRIPT_DIR/start-gateway.sh" > /dev/null 2>&1

    echo -e "${YELLOW}    Waiting for services to be ready...${NC}"
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
    *) echo "Unknown test: $TEST_NAME"; exit 1 ;;
esac

DURATION_METRIC="http_req_duration"
REQS_METRIC="http_reqs"
FAILED_METRIC="http_req_failed"
ITER_METRIC="iterations"
DURATION_FALLBACK=""
REQS_FALLBACK=""
FAILED_FALLBACK=""
ITER_FALLBACK=""

# ---------------------------------------------------------------------------
# Start infrastructure
# ---------------------------------------------------------------------------

start_infra() {
    start_infrastructure_gateway
}

# Run k6 with the k6 mode, run duration and optional VU count
run_k6() {
    local summary_file=$1
    local k6_args=(-e "MODE=$K6_MODE" -e "BENCH_OVER_TIME=${MEASURE_SECONDS}s")
    if [ -n "${BENCH_VUS:-}" ]; then
        k6_args+=(-e "BENCH_VUS=$BENCH_VUS")
    fi
    maybe_taskset "$K6_CPUSET" k6 run "${k6_args[@]}" --summary-export="$summary_file" "$K6_SCRIPT"
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

start_infra

echo -e "${BLUE}Warmup run (${MEASURE_SECONDS}s, discarded)...${NC}"
run_k6 "/tmp/benchmark-summary-warmup.json" > /dev/null 2>&1 || true

echo -e "${BLUE}Running ${NUM_RUNS} measured runs for median calculation...${NC}"

declare -a MIN_V P50_V MAX_V AVG_V P90_V P95_V RPS_V ERR_V ITER_V

for i in $(seq 1 $NUM_RUNS); do
    echo -e "${YELLOW}  Run $i/$NUM_RUNS${NC}"
    run_k6 "/tmp/benchmark-summary-${i}.json"

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

stop_infrastructure

RESULT_MIN=$(calculate_median "${MIN_V[@]}")
RESULT_P50=$(calculate_median "${P50_V[@]}")
RESULT_MAX=$(calculate_median "${MAX_V[@]}")
RESULT_AVG=$(calculate_median "${AVG_V[@]}")
RESULT_P90=$(calculate_median "${P90_V[@]}")
RESULT_P95=$(calculate_median "${P95_V[@]}")
RESULT_RPS=$(calculate_median "${RPS_V[@]}")
RESULT_ERR=$(calculate_median "${ERR_V[@]}")
RESULT_ITER=$(calculate_median "${ITER_V[@]}")

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
  "num_runs": ${NUM_RUNS},
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
rm -f /tmp/benchmark-summary-warmup.json

echo ""
echo -e "${GREEN}Benchmark complete!${NC}"
