#!/bin/bash

set -eo pipefail

CURRENT_FILE="$1"
BASELINE_FILE="$2"
OUTPUT_MD="${3:-performance-report.md}"

if [ ! -f "$CURRENT_FILE" ]; then
    echo "Error: Current performance file not found: $CURRENT_FILE"
    exit 1
fi

if [ ! -f "$BASELINE_FILE" ]; then
    echo "Warning: Baseline file not found: $BASELINE_FILE"
    echo "Generating report without comparison..."
    BASELINE_FILE=""
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

# Function to calculate percentage change
calc_change() {
    local current=$1
    local baseline=$2

    if [ "$baseline" == "0" ] || [ -z "$baseline" ]; then
        echo "0"
        return
    fi

    awk "BEGIN {printf \"%.2f\", (($current - $baseline) / $baseline) * 100}"
}

# Function to get emoji for performance change
get_emoji() {
    local change=$1
    local metric_type=$2  # "latency" or "throughput"

    local abs_change=$(echo "$change" | tr -d '-')
    local is_negative=$(echo "$change" | grep -q '^-' && echo "yes" || echo "no")

    # For throughput: higher is better
    if [ "$metric_type" == "throughput" ]; then
        if [ "$is_negative" == "no" ] && (( $(echo "$abs_change > 10" | bc -l) )); then
            echo "🎉"  # Significant improvement
        elif [ "$is_negative" == "no" ] && (( $(echo "$abs_change > 5" | bc -l) )); then
            echo "✅"  # Moderate improvement
        elif [ "$is_negative" == "yes" ] && (( $(echo "$abs_change > 10" | bc -l) )); then
            echo "⚠️"  # Significant regression
        else
            echo "✅"  # No significant change or small regression
        fi
    else
        echo "-"
    fi
}

# Function to format change
format_change() {
    local change=$1
    local metric_type=$2  # "throughput"

    if [ "$change" == "0" ] || [ -z "$change" ]; then
        echo "(no change)"
    else
        local is_positive=$(echo "$change > 0" | bc -l)
        local abs_change=$(echo "$change" | tr -d '-')

        # For throughput: higher is better (positive change = better)
        if [ "$metric_type" == "throughput" ]; then
            if [ "$is_positive" == "1" ]; then
                echo "(+${change}%)"
            else
                echo "(${change}%)"
            fi
        else
            echo "(${change}%)"
        fi
    fi
}

# Start building markdown report
cat > "$OUTPUT_MD" <<'EOF'
## 🚀 Fusion Gateway Performance Results

EOF

# Extract current metrics (raw values for calculations)
CURRENT_RPS_RAW=$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".throughput.requests_per_second")
CURRENT_MIN_RAW=$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.min")
CURRENT_P50_RAW=$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.p50")
CURRENT_MAX_RAW=$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.max")
CURRENT_AVG_RAW=$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.avg")
CURRENT_P90_RAW=$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.p90")
CURRENT_P95_RAW=$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.p95")
CURRENT_P99_RAW=$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".response_time.p99")
CURRENT_ERR_RAW=$(get_value "$CURRENT_FILE" ".tests.\"no-recursion\".reliability.error_rate")

# Format current metrics for display
CURRENT_RPS=$(format_number "$CURRENT_RPS_RAW")
CURRENT_MIN=$(format_number "$CURRENT_MIN_RAW")
CURRENT_P50=$(format_number "$CURRENT_P50_RAW")
CURRENT_MAX=$(format_number "$CURRENT_MAX_RAW")
CURRENT_AVG=$(format_number "$CURRENT_AVG_RAW")
CURRENT_P90=$(format_number "$CURRENT_P90_RAW")
CURRENT_P95=$(format_number "$CURRENT_P95_RAW")
CURRENT_P99=$(format_number "$CURRENT_P99_RAW")
CURRENT_ERR=$(format_number "$CURRENT_ERR_RAW")

# If baseline exists, calculate comparisons
if [ -n "$BASELINE_FILE" ]; then
    BASELINE_RPS_RAW=$(get_value "$BASELINE_FILE" ".tests.\"no-recursion\".throughput.requests_per_second")
    BASELINE_MIN_RAW=$(get_value "$BASELINE_FILE" ".tests.\"no-recursion\".response_time.min")
    BASELINE_P50_RAW=$(get_value "$BASELINE_FILE" ".tests.\"no-recursion\".response_time.p50")
    BASELINE_MAX_RAW=$(get_value "$BASELINE_FILE" ".tests.\"no-recursion\".response_time.max")
    BASELINE_AVG_RAW=$(get_value "$BASELINE_FILE" ".tests.\"no-recursion\".response_time.avg")
    BASELINE_P90_RAW=$(get_value "$BASELINE_FILE" ".tests.\"no-recursion\".response_time.p90")
    BASELINE_P95_RAW=$(get_value "$BASELINE_FILE" ".tests.\"no-recursion\".response_time.p95")
    BASELINE_P99_RAW=$(get_value "$BASELINE_FILE" ".tests.\"no-recursion\".response_time.p99")

    # Format baseline values for display
    BASELINE_RPS=$(format_number "$BASELINE_RPS_RAW")
    BASELINE_MIN=$(format_number "$BASELINE_MIN_RAW")
    BASELINE_P50=$(format_number "$BASELINE_P50_RAW")
    BASELINE_MAX=$(format_number "$BASELINE_MAX_RAW")
    BASELINE_AVG=$(format_number "$BASELINE_AVG_RAW")
    BASELINE_P90=$(format_number "$BASELINE_P90_RAW")
    BASELINE_P95=$(format_number "$BASELINE_P95_RAW")
    BASELINE_P99=$(format_number "$BASELINE_P99_RAW")

    # Calculate changes using raw values
    CHANGE_RPS=$(calc_change "$CURRENT_RPS_RAW" "$BASELINE_RPS_RAW")
    CHANGE_MIN=$(calc_change "$CURRENT_MIN_RAW" "$BASELINE_MIN_RAW")
    CHANGE_P50=$(calc_change "$CURRENT_P50_RAW" "$BASELINE_P50_RAW")
    CHANGE_MAX=$(calc_change "$CURRENT_MAX_RAW" "$BASELINE_MAX_RAW")
    CHANGE_AVG=$(calc_change "$CURRENT_AVG_RAW" "$BASELINE_AVG_RAW")
    CHANGE_P90=$(calc_change "$CURRENT_P90_RAW" "$BASELINE_P90_RAW")
    CHANGE_P95=$(calc_change "$CURRENT_P95_RAW" "$BASELINE_P95_RAW")
    CHANGE_P99=$(calc_change "$CURRENT_P99_RAW" "$BASELINE_P99_RAW")

    # Add comparison section focusing on throughput
    cat >> "$OUTPUT_MD" <<EOF
### ⚡ Throughput (Simple Composite Query Test)

| Metric | Current | Baseline | Change |
|--------|---------|----------|--------|
| **Requests/sec** | ${CURRENT_RPS} req/s | ${BASELINE_RPS} req/s | $(get_emoji "$CHANGE_RPS" "throughput") $(format_change "$CHANGE_RPS" "throughput") |

<details>
<summary>📊 Detailed Response Time Metrics</summary>

#### Current

| Min | Med | Max | Avg | P90 | P95 | P99 |
|-----|-----|-----|-----|-----|-----|-----|
| ${CURRENT_MIN}ms | ${CURRENT_P50}ms | ${CURRENT_MAX}ms | ${CURRENT_AVG}ms | ${CURRENT_P90}ms | ${CURRENT_P95}ms | ${CURRENT_P99}ms |

#### Baseline

| Min | Med | Max | Avg | P90 | P95 | P99 |
|-----|-----|-----|-----|-----|-----|-----|
| ${BASELINE_MIN}ms | ${BASELINE_P50}ms | ${BASELINE_MAX}ms | ${BASELINE_AVG}ms | ${BASELINE_P90}ms | ${BASELINE_P95}ms | ${BASELINE_P99}ms |

#### Change vs Baseline

| Min | Med | Max | Avg | P90 | P95 | P99 |
|-----|-----|-----|-----|-----|-----|-----|
| ${CHANGE_MIN}% | ${CHANGE_P50}% | ${CHANGE_MAX}% | ${CHANGE_AVG}% | ${CHANGE_P90}% | ${CHANGE_P95}% | ${CHANGE_P99}% |

</details>

### 🎯 Reliability

| Error Rate |
|------------|
| ${CURRENT_ERR}% |

### 🔍 Analysis

EOF

    # Determine overall assessment based on throughput change
    SIGNIFICANT_REGRESSION=false

    abs_change=$(echo "$CHANGE_RPS" | tr -d '-')
    if (( $(echo "$abs_change > 10" | bc -l) )) && (( $(echo "$CHANGE_RPS < 0" | bc -l) )); then
        SIGNIFICANT_REGRESSION=true
    fi

    if [ "$SIGNIFICANT_REGRESSION" == "true" ]; then
        echo "⚠️ **Performance regression detected.** Throughput has decreased by more than 10% compared to baseline." >> "$OUTPUT_MD"
    else
        echo "✅ No significant performance regression detected" >> "$OUTPUT_MD"
    fi

else
    # No baseline - just show current metrics
    cat >> "$OUTPUT_MD" <<EOF
### ⚡ Throughput (Simple Composite Query Test)

| Requests/sec |
|--------------|
| ${CURRENT_RPS} req/s |

<details>
<summary>📊 Response Time Metrics</summary>

| Min | Med | Max | Avg | P90 | P95 | P99 |
|-----|-----|-----|-----|-----|-----|-----|
| ${CURRENT_MIN}ms | ${CURRENT_P50}ms | ${CURRENT_MAX}ms | ${CURRENT_AVG}ms | ${CURRENT_P90}ms | ${CURRENT_P95}ms | ${CURRENT_P99}ms |

</details>

### 🎯 Reliability

| Error Rate |
|------------|
| ${CURRENT_ERR}% |

_No baseline data available for comparison._

EOF
fi

echo "Performance report generated: $OUTPUT_MD"
cat "$OUTPUT_MD"
