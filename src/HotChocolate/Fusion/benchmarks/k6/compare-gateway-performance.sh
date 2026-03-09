#!/bin/bash

set -eo pipefail

CURRENT_FILE="$1"
BASELINE_FILE="$2"
OUTPUT_MD="${3:-performance-report.md}"
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [ ! -f "$CURRENT_FILE" ]; then
    echo "Error: Current performance file not found: $CURRENT_FILE"
    exit 1
fi

HAS_BASELINE=false
if [ -n "$BASELINE_FILE" ] && [ -f "$BASELINE_FILE" ]; then
    HAS_BASELINE=true
fi

# Function to extract value from JSON
get_value() {
    local file=$1
    local path=$2

    if command -v jq &> /dev/null; then
        jq -r "$path // 0" "$file"
    else
        # Fallback parsing without jq
        grep -A 100 "$(echo "$path" | cut -d'.' -f2)" "$file" | grep "$(echo "$path" | cut -d'.' -f3-)" | grep -oE '[0-9]+\.[0-9]+' | head -1 || echo "0"
    fi
}

# Function to format number to 2 decimal places
format_number() {
    local num=$1
    printf "%.2f" "$num" 2>/dev/null || echo "$num"
}

# Extract metrics for no-recursion test - constant mode
NO_REC_RPS=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".constant.throughput.requests_per_second")")
NO_REC_MIN=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".constant.response_time.min")")
NO_REC_P50=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".constant.response_time.p50")")
NO_REC_MAX=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".constant.response_time.max")")
NO_REC_AVG=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".constant.response_time.avg")")
NO_REC_P90=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".constant.response_time.p90")")
NO_REC_P95=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".constant.response_time.p95")")
NO_REC_ERR=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".constant.reliability.error_rate")")

# Extract metrics for no-recursion test - ramping mode
NO_REC_RAMP_RPS=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".ramping.throughput.requests_per_second")")
NO_REC_RAMP_MIN=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".ramping.response_time.min")")
NO_REC_RAMP_P50=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".ramping.response_time.p50")")
NO_REC_RAMP_MAX=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".ramping.response_time.max")")
NO_REC_RAMP_AVG=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".ramping.response_time.avg")")
NO_REC_RAMP_P90=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".ramping.response_time.p90")")
NO_REC_RAMP_P95=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".ramping.response_time.p95")")
NO_REC_RAMP_ERR=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".ramping.reliability.error_rate")")

# Extract metrics for deep-recursion test - constant mode
DEEP_REC_RPS=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".constant.throughput.requests_per_second")")
DEEP_REC_MIN=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".constant.response_time.min")")
DEEP_REC_P50=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".constant.response_time.p50")")
DEEP_REC_MAX=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".constant.response_time.max")")
DEEP_REC_AVG=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".constant.response_time.avg")")
DEEP_REC_P90=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".constant.response_time.p90")")
DEEP_REC_P95=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".constant.response_time.p95")")
DEEP_REC_ERR=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".constant.reliability.error_rate")")

# Extract metrics for deep-recursion test - ramping mode
DEEP_REC_RAMP_RPS=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".ramping.throughput.requests_per_second")")
DEEP_REC_RAMP_MIN=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".ramping.response_time.min")")
DEEP_REC_RAMP_P50=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".ramping.response_time.p50")")
DEEP_REC_RAMP_MAX=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".ramping.response_time.max")")
DEEP_REC_RAMP_AVG=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".ramping.response_time.avg")")
DEEP_REC_RAMP_P90=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".ramping.response_time.p90")")
DEEP_REC_RAMP_P95=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".ramping.response_time.p95")")
DEEP_REC_RAMP_ERR=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"deep-recursion\".ramping.reliability.error_rate")")

# Extract metrics for variable-batch-throughput test - constant mode
VAR_BATCH_RPS=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".constant.throughput.requests_per_second")")
VAR_BATCH_MIN=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".constant.response_time.min")")
VAR_BATCH_P50=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".constant.response_time.p50")")
VAR_BATCH_MAX=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".constant.response_time.max")")
VAR_BATCH_AVG=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".constant.response_time.avg")")
VAR_BATCH_P90=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".constant.response_time.p90")")
VAR_BATCH_P95=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".constant.response_time.p95")")
VAR_BATCH_ERR=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".constant.reliability.error_rate")")

# Extract metrics for variable-batch-throughput test - ramping mode
VAR_BATCH_RAMP_RPS=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".ramping.throughput.requests_per_second")")
VAR_BATCH_RAMP_MIN=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".ramping.response_time.min")")
VAR_BATCH_RAMP_P50=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".ramping.response_time.p50")")
VAR_BATCH_RAMP_MAX=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".ramping.response_time.max")")
VAR_BATCH_RAMP_AVG=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".ramping.response_time.avg")")
VAR_BATCH_RAMP_P90=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".ramping.response_time.p90")")
VAR_BATCH_RAMP_P95=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".ramping.response_time.p95")")
VAR_BATCH_RAMP_ERR=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".ramping.reliability.error_rate")")

# Start building markdown report
cat > "$OUTPUT_MD" <<EOF
### Fusion Gateway Performance Results

#### Simple Composite Query

| | Req/s | Err% |
|:--|--:|--:|
| **Constant** (50 VUs) | ${NO_REC_RPS} | ${NO_REC_ERR}% |
| **Ramping** (0-500-0 VUs) | ${NO_REC_RAMP_RPS} | ${NO_REC_RAMP_ERR}% |

<details>
<summary>Response Times & Query</summary>

| | Min | Med | Avg | P90 | P95 | Max |
|:--|--:|--:|--:|--:|--:|--:|
| **Constant** | ${NO_REC_MIN}ms | ${NO_REC_P50}ms | ${NO_REC_AVG}ms | ${NO_REC_P90}ms | ${NO_REC_P95}ms | ${NO_REC_MAX}ms |
| **Ramping** | ${NO_REC_RAMP_MIN}ms | ${NO_REC_RAMP_P50}ms | ${NO_REC_RAMP_AVG}ms | ${NO_REC_RAMP_P90}ms | ${NO_REC_RAMP_P95}ms | ${NO_REC_RAMP_MAX}ms |

\`\`\`graphql
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
\`\`\`

</details>

---

#### Deep Recursion Query

| | Req/s | Err% |
|:--|--:|--:|
| **Constant** (50 VUs) | ${DEEP_REC_RPS} | ${DEEP_REC_ERR}% |
| **Ramping** (0-500-0 VUs) | ${DEEP_REC_RAMP_RPS} | ${DEEP_REC_RAMP_ERR}% |

<details>
<summary>Response Times & Query</summary>

| | Min | Med | Avg | P90 | P95 | Max |
|:--|--:|--:|--:|--:|--:|--:|
| **Constant** | ${DEEP_REC_MIN}ms | ${DEEP_REC_P50}ms | ${DEEP_REC_AVG}ms | ${DEEP_REC_P90}ms | ${DEEP_REC_P95}ms | ${DEEP_REC_MAX}ms |
| **Ramping** | ${DEEP_REC_RAMP_MIN}ms | ${DEEP_REC_RAMP_P50}ms | ${DEEP_REC_RAMP_AVG}ms | ${DEEP_REC_RAMP_P90}ms | ${DEEP_REC_RAMP_P95}ms | ${DEEP_REC_RAMP_MAX}ms |

\`\`\`graphql
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
\`\`\`

</details>

---

#### Variable Batching Throughput

| | Req/s | Err% |
|:--|--:|--:|
| **Constant** (50 VUs) | ${VAR_BATCH_RPS} | ${VAR_BATCH_ERR}% |
| **Ramping** (0-500-0 VUs) | ${VAR_BATCH_RAMP_RPS} | ${VAR_BATCH_RAMP_ERR}% |

<details>
<summary>Response Times & Query</summary>

| | Min | Med | Avg | P90 | P95 | Max |
|:--|--:|--:|--:|--:|--:|--:|
| **Constant** | ${VAR_BATCH_MIN}ms | ${VAR_BATCH_P50}ms | ${VAR_BATCH_AVG}ms | ${VAR_BATCH_P90}ms | ${VAR_BATCH_P95}ms | ${VAR_BATCH_MAX}ms |
| **Ramping** | ${VAR_BATCH_RAMP_MIN}ms | ${VAR_BATCH_RAMP_P50}ms | ${VAR_BATCH_RAMP_AVG}ms | ${VAR_BATCH_RAMP_P90}ms | ${VAR_BATCH_RAMP_P95}ms | ${VAR_BATCH_RAMP_MAX}ms |

\`\`\`graphql
query TestQuery(\$upc: ID!, \$price: Long!, \$weight: Long!) {
  productByUpc(upc: \$upc) {
    inStock
    shippingEstimate(weight: \$weight, price: \$price)
  }
}
\`\`\`

**Variables** (5 sets batched per request)

\`\`\`json
[
  { "upc": "1", "price": 899, "weight": 100 },
  { "upc": "2", "price": 1299, "weight": 1000 },
  { "upc": "3", "price": 15, "weight": 20 },
  { "upc": "4", "price": 499, "weight": 100 },
  { "upc": "5", "price": 1299, "weight": 1000 }
]
\`\`\`

</details>
EOF

echo "Performance report generated: $OUTPUT_MD"
cat "$OUTPUT_MD"
