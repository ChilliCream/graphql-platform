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

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}k6 Performance Test Collector${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""
echo "Output file: $OUTPUT_FILE"
echo ""

# Run single-fetch test and capture summary JSON
echo -e "${BLUE}Running Single Fetch Test...${NC}"
k6 run --summary-export=/tmp/single-fetch-summary.json "$SCRIPT_DIR/single-fetch.js"

# Run dataloader test and capture summary JSON
echo -e "${BLUE}Running DataLoader Test...${NC}"
k6 run --summary-export=/tmp/dataloader-summary.json "$SCRIPT_DIR/dataloader.js"

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
        jq -r ".metrics.\"${metric}\".values.${stat} // 0" "$file"
    else
        grep -A 20 "\"${metric}\"" "$file" | grep "\"${stat}\"" | grep -oE '[0-9]+\.[0-9]+' | head -1 || echo "0"
    fi
}

# Extract metrics from single-fetch test
SINGLE_P50=$(extract_metric /tmp/single-fetch-summary.json "http_req_duration{phase:measurement}" "p(50)")
SINGLE_P95=$(extract_metric /tmp/single-fetch-summary.json "http_req_duration{phase:measurement}" "p(95)")
SINGLE_P99=$(extract_metric /tmp/single-fetch-summary.json "http_req_duration{phase:measurement}" "p(99)")
SINGLE_AVG=$(extract_metric /tmp/single-fetch-summary.json "http_req_duration{phase:measurement}" "avg")
SINGLE_RPS=$(extract_metric /tmp/single-fetch-summary.json "http_reqs" "rate")
SINGLE_ERROR_RATE=$(extract_metric /tmp/single-fetch-summary.json "http_req_failed{phase:measurement}" "rate")
SINGLE_ITERATIONS=$(extract_metric /tmp/single-fetch-summary.json "iterations" "count")

# Extract metrics from dataloader test
DATALOADER_P50=$(extract_metric /tmp/dataloader-summary.json "http_req_duration{phase:measurement}" "p(50)")
DATALOADER_P95=$(extract_metric /tmp/dataloader-summary.json "http_req_duration{phase:measurement}" "p(95)")
DATALOADER_P99=$(extract_metric /tmp/dataloader-summary.json "http_req_duration{phase:measurement}" "p(99)")
DATALOADER_AVG=$(extract_metric /tmp/dataloader-summary.json "http_req_duration{phase:measurement}" "avg")
DATALOADER_RPS=$(extract_metric /tmp/dataloader-summary.json "http_reqs" "rate")
DATALOADER_ERROR_RATE=$(extract_metric /tmp/dataloader-summary.json "http_req_failed{phase:measurement}" "rate")
DATALOADER_ITERATIONS=$(extract_metric /tmp/dataloader-summary.json "iterations" "count")

# Create JSON output
cat > "$OUTPUT_FILE" <<EOF
{
  "timestamp": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
  "tests": {
    "single-fetch": {
      "name": "Single Fetch (50 products, names only)",
      "response_time": {
        "p50": ${SINGLE_P50},
        "p95": ${SINGLE_P95},
        "p99": ${SINGLE_P99},
        "avg": ${SINGLE_AVG}
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
        "p50": ${DATALOADER_P50},
        "p95": ${DATALOADER_P95},
        "p99": ${DATALOADER_P99},
        "avg": ${DATALOADER_AVG}
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
rm -f /tmp/single-fetch-summary.json /tmp/dataloader-summary.json

echo ""
echo -e "${GREEN}Performance test collection complete!${NC}"
