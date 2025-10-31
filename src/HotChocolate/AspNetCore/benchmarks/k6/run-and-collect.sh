#!/bin/bash

set -eo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
OUTPUT_FILE="${1:-$SCRIPT_DIR/performance-data.json}"
NUM_RUNS=3

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}k6 Performance Test Collector${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo "Output file: $OUTPUT_FILE"
echo "Running each test ${NUM_RUNS} times to reduce variance"
echo ""

# Function to calculate median of 3 values
calculate_median() {
    local values=("$@")
    # Sort the values
    IFS=$'\n' sorted=($(sort -n <<<"${values[*]}"))
    unset IFS
    # Return the middle value
    echo "${sorted[1]}"
}

# Run single-fetch test 3 times
echo -e "${BLUE}Running Single Fetch Test (${NUM_RUNS} runs)...${NC}"
for i in $(seq 1 $NUM_RUNS); do
    echo -e "${YELLOW}  Run $i/$NUM_RUNS${NC}"
    k6 run --summary-export=/tmp/single-fetch-summary-${i}.json "$SCRIPT_DIR/single-fetch.js"
done

# Run dataloader test 3 times
echo -e "${BLUE}Running DataLoader Test (${NUM_RUNS} runs)...${NC}"
for i in $(seq 1 $NUM_RUNS); do
    echo -e "${YELLOW}  Run $i/$NUM_RUNS${NC}"
    k6 run --summary-export=/tmp/dataloader-summary-${i}.json "$SCRIPT_DIR/dataloader.js"
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

# Extract metrics from single-fetch test (all runs) and calculate medians
echo -e "${YELLOW}Calculating median values from ${NUM_RUNS} runs...${NC}"

# Arrays to store values from each run
declare -a SINGLE_MIN_VALUES SINGLE_P50_VALUES SINGLE_MAX_VALUES SINGLE_AVG_VALUES
declare -a SINGLE_P90_VALUES SINGLE_P95_VALUES SINGLE_P99_VALUES SINGLE_RPS_VALUES
declare -a SINGLE_ERROR_VALUES SINGLE_ITERATIONS_VALUES

# Extract metrics from each run
for i in $(seq 1 $NUM_RUNS); do
    file="/tmp/single-fetch-summary-${i}.json"

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "min")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "min")
    SINGLE_MIN_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(50)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(50)")
    SINGLE_P50_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "max")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "max")
    SINGLE_MAX_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "avg")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "avg")
    SINGLE_AVG_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(90)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(90)")
    SINGLE_P90_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(95)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(95)")
    SINGLE_P95_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(99)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(99)")
    SINGLE_P99_VALUES+=("$val")

    val=$(extract_metric "$file" "http_reqs{phase:measurement}" "rate")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_reqs" "rate")
    SINGLE_RPS_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_failed{phase:measurement}" "rate")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_failed" "rate")
    SINGLE_ERROR_VALUES+=("$val")

    val=$(extract_metric "$file" "iterations{phase:measurement}" "count")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "iterations" "count")
    SINGLE_ITERATIONS_VALUES+=("$val")
done

# Calculate medians
SINGLE_MIN=$(calculate_median "${SINGLE_MIN_VALUES[@]}")
SINGLE_P50=$(calculate_median "${SINGLE_P50_VALUES[@]}")
SINGLE_MAX=$(calculate_median "${SINGLE_MAX_VALUES[@]}")
SINGLE_AVG=$(calculate_median "${SINGLE_AVG_VALUES[@]}")
SINGLE_P90=$(calculate_median "${SINGLE_P90_VALUES[@]}")
SINGLE_P95=$(calculate_median "${SINGLE_P95_VALUES[@]}")
SINGLE_P99=$(calculate_median "${SINGLE_P99_VALUES[@]}")
SINGLE_RPS=$(calculate_median "${SINGLE_RPS_VALUES[@]}")
SINGLE_ERROR_RATE=$(calculate_median "${SINGLE_ERROR_VALUES[@]}")
SINGLE_ITERATIONS=$(calculate_median "${SINGLE_ITERATIONS_VALUES[@]}")

# Extract metrics from dataloader test (all runs) and calculate medians
declare -a DATALOADER_MIN_VALUES DATALOADER_P50_VALUES DATALOADER_MAX_VALUES DATALOADER_AVG_VALUES
declare -a DATALOADER_P90_VALUES DATALOADER_P95_VALUES DATALOADER_P99_VALUES DATALOADER_RPS_VALUES
declare -a DATALOADER_ERROR_VALUES DATALOADER_ITERATIONS_VALUES

# Extract metrics from each run
for i in $(seq 1 $NUM_RUNS); do
    file="/tmp/dataloader-summary-${i}.json"

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "min")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "min")
    DATALOADER_MIN_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(50)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(50)")
    DATALOADER_P50_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "max")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "max")
    DATALOADER_MAX_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "avg")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "avg")
    DATALOADER_AVG_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(90)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(90)")
    DATALOADER_P90_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(95)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(95)")
    DATALOADER_P95_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_duration{phase:measurement}" "p(99)")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_duration" "p(99)")
    DATALOADER_P99_VALUES+=("$val")

    val=$(extract_metric "$file" "http_reqs{phase:measurement}" "rate")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_reqs" "rate")
    DATALOADER_RPS_VALUES+=("$val")

    val=$(extract_metric "$file" "http_req_failed{phase:measurement}" "rate")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "http_req_failed" "rate")
    DATALOADER_ERROR_VALUES+=("$val")

    val=$(extract_metric "$file" "iterations{phase:measurement}" "count")
    [ "$val" == "0" ] && val=$(extract_metric "$file" "iterations" "count")
    DATALOADER_ITERATIONS_VALUES+=("$val")
done

# Calculate medians
DATALOADER_MIN=$(calculate_median "${DATALOADER_MIN_VALUES[@]}")
DATALOADER_P50=$(calculate_median "${DATALOADER_P50_VALUES[@]}")
DATALOADER_MAX=$(calculate_median "${DATALOADER_MAX_VALUES[@]}")
DATALOADER_AVG=$(calculate_median "${DATALOADER_AVG_VALUES[@]}")
DATALOADER_P90=$(calculate_median "${DATALOADER_P90_VALUES[@]}")
DATALOADER_P95=$(calculate_median "${DATALOADER_P95_VALUES[@]}")
DATALOADER_P99=$(calculate_median "${DATALOADER_P99_VALUES[@]}")
DATALOADER_RPS=$(calculate_median "${DATALOADER_RPS_VALUES[@]}")
DATALOADER_ERROR_RATE=$(calculate_median "${DATALOADER_ERROR_VALUES[@]}")
DATALOADER_ITERATIONS=$(calculate_median "${DATALOADER_ITERATIONS_VALUES[@]}")

# Create JSON output
cat > "$OUTPUT_FILE" <<EOF
{
  "timestamp": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "num_runs": ${NUM_RUNS},
  "note": "All metrics are median values from ${NUM_RUNS} test runs",
  "tests": {
    "single-fetch": {
      "name": "Single Fetch (50 products, names only)",
      "response_time": {
        "min": ${SINGLE_MIN},
        "p50": ${SINGLE_P50},
        "max": ${SINGLE_MAX},
        "avg": ${SINGLE_AVG},
        "p90": ${SINGLE_P90},
        "p95": ${SINGLE_P95},
        "p99": ${SINGLE_P99}
      },
      "throughput": {
        "requests_per_second": ${SINGLE_RPS},
        "total_iterations": ${SINGLE_ITERATIONS}
      },
      "reliability": {
        "error_rate": ${SINGLE_ERROR_RATE}
      }
    },
    "dataloader": {
      "name": "DataLoader (50 products with brands)",
      "response_time": {
        "min": ${DATALOADER_MIN},
        "p50": ${DATALOADER_P50},
        "max": ${DATALOADER_MAX},
        "avg": ${DATALOADER_AVG},
        "p90": ${DATALOADER_P90},
        "p95": ${DATALOADER_P95},
        "p99": ${DATALOADER_P99}
      },
      "throughput": {
        "requests_per_second": ${DATALOADER_RPS},
        "total_iterations": ${DATALOADER_ITERATIONS}
      },
      "reliability": {
        "error_rate": ${DATALOADER_ERROR_RATE}
      }
    }
  }
}
EOF

echo -e "${GREEN}✓${NC} Performance data written to $OUTPUT_FILE"
cat "$OUTPUT_FILE"

# Clean up temp files
for i in $(seq 1 $NUM_RUNS); do
    rm -f /tmp/single-fetch-summary-${i}.json
    rm -f /tmp/dataloader-summary-${i}.json
done

echo ""
echo -e "${GREEN}Performance test collection complete!${NC}"
echo -e "${YELLOW}Note: All metrics are median values from ${NUM_RUNS} test runs${NC}"
