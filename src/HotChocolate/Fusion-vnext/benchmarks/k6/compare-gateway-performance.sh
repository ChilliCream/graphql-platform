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

# Extract metrics for no-recursion test
NO_REC_RPS=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".throughput.requests_per_second")")
NO_REC_MIN=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.min")")
NO_REC_P50=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.p50")")
NO_REC_MAX=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.max")")
NO_REC_AVG=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.avg")")
NO_REC_P90=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.p90")")
NO_REC_P95=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.p95")")
NO_REC_P99=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.p99")")
NO_REC_ERR=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".reliability.error_rate")")

# Extract metrics for variable-batch-throughput test
VAR_BATCH_RPS=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".throughput.requests_per_second")")
VAR_BATCH_MIN=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".response_time.min")")
VAR_BATCH_P50=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".response_time.p50")")
VAR_BATCH_MAX=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".response_time.max")")
VAR_BATCH_AVG=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".response_time.avg")")
VAR_BATCH_P90=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".response_time.p90")")
VAR_BATCH_P95=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".response_time.p95")")
VAR_BATCH_P99=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".response_time.p99")")
VAR_BATCH_ERR=$(format_number "$(get_value "$CURRENT_FILE" ".tests.\"variable-batch-throughput\".reliability.error_rate")")

# Start building markdown report
cat > "$OUTPUT_MD" <<'EOF'
### 🚀 Fusion Gateway Performance Results

#### Simple Composite Query

EOF

# Add table with metrics
cat >> "$OUTPUT_MD" <<EOF
| Requests/sec | Error Rate |
|--------------|------------|
| ${NO_REC_RPS} req/s | ${NO_REC_ERR}% |

<details>
<summary>📊 Response Time Metrics</summary>

| Min | Med | Max | Avg | P90 | P95 | P99 |
|-----|-----|-----|-----|-----|-----|-----|
| ${NO_REC_MIN}ms | ${NO_REC_P50}ms | ${NO_REC_MAX}ms | ${NO_REC_AVG}ms | ${NO_REC_P90}ms | ${NO_REC_P95}ms | ${NO_REC_P99}ms |

**Executed Query**

\`\`\`graphql
fragment User on User {
  id
  username
  name
}

fragment Review on Review {
  id
  body
}

fragment Product on Product {
  inStock
  name
  price
  shippingEstimate
  upc
  weight
}

query TestQuery {
  topProducts(first: 5) {
    ...Product
    reviews {
      ...Review
      author {
        ...User
      }
    }
  }
}
\`\`\`

</details>

#### Variable Batching Throughput

| Requests/sec | Error Rate |
|--------------|------------|
| ${VAR_BATCH_RPS} req/s | ${VAR_BATCH_ERR}% |

<details>
<summary>📊 Response Time Metrics</summary>

| Min | Med | Max | Avg | P90 | P95 | P99 |
|-----|-----|-----|-----|-----|-----|-----|
| ${VAR_BATCH_MIN}ms | ${VAR_BATCH_P50}ms | ${VAR_BATCH_MAX}ms | ${VAR_BATCH_AVG}ms | ${VAR_BATCH_P90}ms | ${VAR_BATCH_P95}ms | ${VAR_BATCH_P99}ms |

**Executed Query**

\`\`\`graphql
query TestQuery_8f7a46ce_2(
  \$__fusion_1_upc: ID!
  \$__fusion_2_price: Long!
  \$__fusion_2_weight: Long!
) {
  productByUpc(upc: \$__fusion_1_upc) {
    inStock
    shippingEstimate(weight: \$__fusion_2_weight, price: \$__fusion_2_price)
  }
}
\`\`\`

**Variables** (5 sets batched in single request)

\`\`\`json
[
  { "__fusion_1_upc": "1", "__fusion_2_price": 899, "__fusion_2_weight": 100 },
  { "__fusion_1_upc": "2", "__fusion_2_price": 1299, "__fusion_2_weight": 1000 },
  { "__fusion_1_upc": "3", "__fusion_2_price": 15, "__fusion_2_weight": 20 },
  { "__fusion_1_upc": "4", "__fusion_2_price": 499, "__fusion_2_weight": 100 },
  { "__fusion_1_upc": "5", "__fusion_2_price": 1299, "__fusion_2_weight": 1000 }
]
\`\`\`

</details>

_No baseline data available for comparison._
EOF

echo "Performance report generated: $OUTPUT_MD"
cat "$OUTPUT_MD"
