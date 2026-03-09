#!/bin/bash

set -eo pipefail

# =============================================================================
# Generate a combined markdown benchmark report from individual result JSONs.
# Designed for progressive updates: shows "pending" for benchmarks not yet done.
#
# Usage: ./generate-report.sh <results-dir> <output-md>
#
# Arguments:
#   results-dir: Directory containing benchmark-*/result.json subdirectories
#   output-md:   Path for the generated markdown report
# =============================================================================

RESULTS_DIR="${1:?Usage: $0 <results-dir> <output-md>}"
OUTPUT_MD="${2:?Usage: $0 <results-dir> <output-md>}"

# ---------------------------------------------------------------------------
# Collect all available result files into an associative array
# Key: "test|mode|runner_group"  Value: path to result.json
# ---------------------------------------------------------------------------

declare -A RESULTS

for f in $(find "$RESULTS_DIR" -name "result.json" 2>/dev/null); do
    test_name=$(jq -r '.test' "$f")
    mode=$(jq -r '.mode' "$f")
    runner_group=$(jq -r '.runner_group' "$f")
    key="${test_name}|${mode}|${runner_group}"
    RESULTS["$key"]="$f"
done

echo "Found ${#RESULTS[@]} benchmark result(s)"

# ---------------------------------------------------------------------------
# Helper: format a summary row (Req/s and Err%)
# ---------------------------------------------------------------------------

format_summary_row() {
    local label=$1
    local mode_desc=$2
    local test=$3
    local mode=$4
    local runner_group=$5

    local key="${test}|${mode}|${runner_group}"

    if [ -n "${RESULTS[$key]:-}" ]; then
        local f="${RESULTS[$key]}"
        local rps err
        rps=$(printf "%.2f" "$(jq -r '.throughput.requests_per_second' "$f")")
        err=$(printf "%.2f" "$(jq -r '.reliability.error_rate' "$f")")
        echo "| **${label}** ${mode_desc} | ${rps} | ${err}% |"
    else
        echo "| **${label}** ${mode_desc} | *pending* | *pending* |"
    fi
}

# ---------------------------------------------------------------------------
# Helper: format a detail row (response times)
# ---------------------------------------------------------------------------

format_detail_row() {
    local label=$1
    local test=$2
    local mode=$3
    local runner_group=$4

    local key="${test}|${mode}|${runner_group}"

    if [ -n "${RESULTS[$key]:-}" ]; then
        local f="${RESULTS[$key]}"
        local min p50 avg p90 p95 max
        min=$(printf "%.2f" "$(jq -r '.response_time.min' "$f")")
        p50=$(printf "%.2f" "$(jq -r '.response_time.p50' "$f")")
        avg=$(printf "%.2f" "$(jq -r '.response_time.avg' "$f")")
        p90=$(printf "%.2f" "$(jq -r '.response_time.p90' "$f")")
        p95=$(printf "%.2f" "$(jq -r '.response_time.p95' "$f")")
        max=$(printf "%.2f" "$(jq -r '.response_time.max' "$f")")
        echo "| **${label}** | ${min}ms | ${p50}ms | ${avg}ms | ${p90}ms | ${p95}ms | ${max}ms |"
    else
        echo "| **${label}** | *pending* | *pending* | *pending* | *pending* | *pending* | *pending* |"
    fi
}

# ---------------------------------------------------------------------------
# Count completed / total benchmarks
# ---------------------------------------------------------------------------

TOTAL_BENCHMARKS=12
COMPLETED=${#RESULTS[@]}

# ---------------------------------------------------------------------------
# Generate the markdown report
# ---------------------------------------------------------------------------

{
    echo "### Fusion Gateway Performance Results"
    echo ""
    if [ "$COMPLETED" -lt "$TOTAL_BENCHMARKS" ]; then
        echo "> **Progress: ${COMPLETED}/${TOTAL_BENCHMARKS} benchmarks completed** - this report updates as each job finishes."
        echo ""
    fi

    # ===== Simple Composite Query =====
    echo "#### Simple Composite Query"
    echo ""
    echo "| | Req/s | Err% |"
    echo "|:--|--:|--:|"
    format_summary_row "Constant 1" "(50 VUs)" "no-recursion" "constant" "Benchmarking"
    format_summary_row "Constant 2" "(50 VUs)" "no-recursion" "constant" "Benchmarking-2"
    format_summary_row "Ramping 1" "(0-500-0 VUs)" "no-recursion" "ramping" "Benchmarking"
    format_summary_row "Ramping 2" "(0-500-0 VUs)" "no-recursion" "ramping" "Benchmarking-2"
    echo ""
    echo "<details>"
    echo "<summary>Response Times & Query</summary>"
    echo ""
    echo "| | Min | Med | Avg | P90 | P95 | Max |"
    echo "|:--|--:|--:|--:|--:|--:|--:|"
    format_detail_row "Constant 1" "no-recursion" "constant" "Benchmarking"
    format_detail_row "Constant 2" "no-recursion" "constant" "Benchmarking-2"
    format_detail_row "Ramping 1" "no-recursion" "ramping" "Benchmarking"
    format_detail_row "Ramping 2" "no-recursion" "ramping" "Benchmarking-2"
    echo ""
    cat <<'QUERY'
```graphql
query TestQuery {
  topProducts(first: 5) {
    inStock
    name
    price
    shippingEstimate
    upc
    weight
    reviews {
      id
      body
      author {
        id
        username
        name
      }
    }
  }
}
```
QUERY
    echo ""
    echo "</details>"
    echo ""
    echo "---"
    echo ""

    # ===== Deep Recursion Query =====
    echo "#### Deep Recursion Query"
    echo ""
    echo "| | Req/s | Err% |"
    echo "|:--|--:|--:|"
    format_summary_row "Constant 1" "(50 VUs)" "deep-recursion" "constant" "Benchmarking"
    format_summary_row "Constant 2" "(50 VUs)" "deep-recursion" "constant" "Benchmarking-2"
    format_summary_row "Ramping 1" "(0-500-0 VUs)" "deep-recursion" "ramping" "Benchmarking"
    format_summary_row "Ramping 2" "(0-500-0 VUs)" "deep-recursion" "ramping" "Benchmarking-2"
    echo ""
    echo "<details>"
    echo "<summary>Response Times & Query</summary>"
    echo ""
    echo "| | Min | Med | Avg | P90 | P95 | Max |"
    echo "|:--|--:|--:|--:|--:|--:|--:|"
    format_detail_row "Constant 1" "deep-recursion" "constant" "Benchmarking"
    format_detail_row "Constant 2" "deep-recursion" "constant" "Benchmarking-2"
    format_detail_row "Ramping 1" "deep-recursion" "ramping" "Benchmarking"
    format_detail_row "Ramping 2" "deep-recursion" "ramping" "Benchmarking-2"
    echo ""
    cat <<'QUERY'
```graphql
query TestQuery {
  users {
    id
    username
    name
    reviews {
      id
      body
      product {
        inStock
        name
        price
        shippingEstimate
        upc
        weight
        reviews {
          id
          body
          author {
            id
            username
            name
            reviews {
              id
              body
              product {
                inStock
                name
                price
                shippingEstimate
                upc
                weight
              }
            }
          }
        }
      }
    }
  }
  topProducts(first: 5) {
    inStock
    name
    price
    shippingEstimate
    upc
    weight
    reviews {
      id
      body
      author {
        id
        username
        name
        reviews {
          id
          body
          product {
            inStock
            name
            price
            shippingEstimate
            upc
            weight
          }
        }
      }
    }
  }
}
```
QUERY
    echo ""
    echo "</details>"
    echo ""
    echo "---"
    echo ""

    # ===== Variable Batching Throughput =====
    echo "#### Variable Batching Throughput"
    echo ""
    echo "| | Req/s | Err% |"
    echo "|:--|--:|--:|"
    format_summary_row "Constant 1" "(50 VUs)" "variable-batch" "constant" "Benchmarking"
    format_summary_row "Constant 2" "(50 VUs)" "variable-batch" "constant" "Benchmarking-2"
    format_summary_row "Ramping 1" "(0-500-0 VUs)" "variable-batch" "ramping" "Benchmarking"
    format_summary_row "Ramping 2" "(0-500-0 VUs)" "variable-batch" "ramping" "Benchmarking-2"
    echo ""
    echo "<details>"
    echo "<summary>Response Times & Query</summary>"
    echo ""
    echo "| | Min | Med | Avg | P90 | P95 | Max |"
    echo "|:--|--:|--:|--:|--:|--:|--:|"
    format_detail_row "Constant 1" "variable-batch" "constant" "Benchmarking"
    format_detail_row "Constant 2" "variable-batch" "constant" "Benchmarking-2"
    format_detail_row "Ramping 1" "variable-batch" "ramping" "Benchmarking"
    format_detail_row "Ramping 2" "variable-batch" "ramping" "Benchmarking-2"
    echo ""
    cat <<'QUERY'
```graphql
query TestQuery($upc: ID!, $price: Long!, $weight: Long!) {
  productByUpc(upc: $upc) {
    inStock
    shippingEstimate(weight: $weight, price: $price)
  }
}
```

**Variables** (5 sets batched per request)

```json
[
  { "upc": "1", "price": 899, "weight": 100 },
  { "upc": "2", "price": 1299, "weight": 1000 },
  { "upc": "3", "price": 15, "weight": 20 },
  { "upc": "4", "price": 499, "weight": 100 },
  { "upc": "5", "price": 1299, "weight": 1000 }
]
```
QUERY
    echo ""
    echo "</details>"
    echo ""

    # ===== Runner legend =====
    echo "---"
    echo ""
    echo "*Runner 1 = Benchmarking, Runner 2 = Benchmarking-2*"

} > "$OUTPUT_MD"

echo "Report generated: $OUTPUT_MD"
