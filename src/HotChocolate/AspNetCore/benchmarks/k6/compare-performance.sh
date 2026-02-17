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
    local metric_type=$2  # "latency" or "throughput" or "error"

    local abs_change=$(echo "$change" | tr -d '-')
    local is_negative=$(echo "$change" | grep -q '^-' && echo "yes" || echo "no")

    # For latency: lower is better, for throughput: higher is better
    if [ "$metric_type" == "latency" ]; then
        if [ "$is_negative" == "yes" ] && (( $(echo "$abs_change > 5" | bc -l) )); then
            echo "🎉"  # Significant improvement
        elif [ "$is_negative" == "yes" ]; then
            echo "✅"  # Small improvement
        elif (( $(echo "$abs_change > 10" | bc -l) )); then
            echo "⚠️"  # Significant regression
        elif (( $(echo "$abs_change > 5" | bc -l) )); then
            echo "⚠️"  # Moderate regression
        else
            echo "✅"  # No significant change
        fi
    elif [ "$metric_type" == "throughput" ]; then
        if [ "$is_negative" == "no" ] && (( $(echo "$abs_change > 10" | bc -l) )); then
            echo "🎉"  # Significant improvement
        elif [ "$is_negative" == "no" ] && (( $(echo "$abs_change > 5" | bc -l) )); then
            echo "✅"  # Moderate improvement
        elif [ "$is_negative" == "yes" ] && (( $(echo "$abs_change > 10" | bc -l) )); then
            echo "⚠️"  # Significant regression
        else
            echo "✅"  # No significant change or small regression
        fi
    elif [ "$metric_type" == "error" ]; then
        if (( $(echo "$abs_change > 0.01" | bc -l) )); then
            echo "⚠️"  # Any increase in errors is bad
        else
            echo "✅"
        fi
    else
        echo "-"
    fi
}

# Function to format change
format_change() {
    local change=$1
    local metric_type=$2  # "latency" or "throughput"
    local show_sign=${3:-true}

    if [ "$change" == "0" ] || [ -z "$change" ]; then
        echo "(no change)"
    elif [ "$show_sign" == "true" ]; then
        local is_positive=$(echo "$change > 0" | bc -l)
        local abs_change=$(echo "$change" | tr -d '-')

        # For latency: lower is better (negative change = better)
        # For throughput: higher is better (positive change = better)
        if [ "$metric_type" == "throughput" ]; then
            if [ "$is_positive" == "1" ]; then
                echo "(${change}% better)"
            else
                echo "(${abs_change}% worse)"
            fi
        else
            # Default to latency behavior (lower is better)
            if [ "$is_positive" == "1" ]; then
                echo "(${change}% worse)"
            else
                echo "(${abs_change}% better)"
            fi
        fi
    else
        echo "(${change}%)"
    fi
}

# Start building markdown report
cat > "$OUTPUT_MD" <<'EOF'
## 🚀 Performance Test Results

EOF

# Extract current metrics (raw values for calculations)
CURRENT_SF_MIN_RAW=$(get_value "$CURRENT_FILE" ".tests.\"single-fetch\".response_time.min")
CURRENT_SF_P50_RAW=$(get_value "$CURRENT_FILE" ".tests.\"single-fetch\".response_time.p50")
CURRENT_SF_MAX_RAW=$(get_value "$CURRENT_FILE" ".tests.\"single-fetch\".response_time.max")
CURRENT_SF_AVG_RAW=$(get_value "$CURRENT_FILE" ".tests.\"single-fetch\".response_time.avg")
CURRENT_SF_P90_RAW=$(get_value "$CURRENT_FILE" ".tests.\"single-fetch\".response_time.p90")
CURRENT_SF_P95_RAW=$(get_value "$CURRENT_FILE" ".tests.\"single-fetch\".response_time.p95")
CURRENT_SF_P99_RAW=$(get_value "$CURRENT_FILE" ".tests.\"single-fetch\".response_time.p99")
CURRENT_SF_RPS_RAW=$(get_value "$CURRENT_FILE" ".tests.\"single-fetch\".throughput.requests_per_second")
CURRENT_SF_ERR_RAW=$(get_value "$CURRENT_FILE" ".tests.\"single-fetch\".reliability.error_rate")

CURRENT_DL_MIN_RAW=$(get_value "$CURRENT_FILE" ".tests.dataloader.response_time.min")
CURRENT_DL_P50_RAW=$(get_value "$CURRENT_FILE" ".tests.dataloader.response_time.p50")
CURRENT_DL_MAX_RAW=$(get_value "$CURRENT_FILE" ".tests.dataloader.response_time.max")
CURRENT_DL_AVG_RAW=$(get_value "$CURRENT_FILE" ".tests.dataloader.response_time.avg")
CURRENT_DL_P90_RAW=$(get_value "$CURRENT_FILE" ".tests.dataloader.response_time.p90")
CURRENT_DL_P95_RAW=$(get_value "$CURRENT_FILE" ".tests.dataloader.response_time.p95")
CURRENT_DL_P99_RAW=$(get_value "$CURRENT_FILE" ".tests.dataloader.response_time.p99")
CURRENT_DL_RPS_RAW=$(get_value "$CURRENT_FILE" ".tests.dataloader.throughput.requests_per_second")
CURRENT_DL_ERR_RAW=$(get_value "$CURRENT_FILE" ".tests.dataloader.reliability.error_rate")

# Format current metrics for display
CURRENT_SF_MIN=$(format_number "$CURRENT_SF_MIN_RAW")
CURRENT_SF_P50=$(format_number "$CURRENT_SF_P50_RAW")
CURRENT_SF_MAX=$(format_number "$CURRENT_SF_MAX_RAW")
CURRENT_SF_AVG=$(format_number "$CURRENT_SF_AVG_RAW")
CURRENT_SF_P90=$(format_number "$CURRENT_SF_P90_RAW")
CURRENT_SF_P95=$(format_number "$CURRENT_SF_P95_RAW")
CURRENT_SF_P99=$(format_number "$CURRENT_SF_P99_RAW")
CURRENT_SF_RPS=$(format_number "$CURRENT_SF_RPS_RAW")
CURRENT_SF_ERR=$(format_number "$CURRENT_SF_ERR_RAW")

CURRENT_DL_MIN=$(format_number "$CURRENT_DL_MIN_RAW")
CURRENT_DL_P50=$(format_number "$CURRENT_DL_P50_RAW")
CURRENT_DL_MAX=$(format_number "$CURRENT_DL_MAX_RAW")
CURRENT_DL_AVG=$(format_number "$CURRENT_DL_AVG_RAW")
CURRENT_DL_P90=$(format_number "$CURRENT_DL_P90_RAW")
CURRENT_DL_P95=$(format_number "$CURRENT_DL_P95_RAW")
CURRENT_DL_P99=$(format_number "$CURRENT_DL_P99_RAW")
CURRENT_DL_RPS=$(format_number "$CURRENT_DL_RPS_RAW")
CURRENT_DL_ERR=$(format_number "$CURRENT_DL_ERR_RAW")

# If baseline exists, calculate comparisons
if [ -n "$BASELINE_FILE" ]; then
    BASELINE_SF_MIN_RAW=$(get_value "$BASELINE_FILE" ".tests.\"single-fetch\".response_time.min")
    BASELINE_SF_P50_RAW=$(get_value "$BASELINE_FILE" ".tests.\"single-fetch\".response_time.p50")
    BASELINE_SF_MAX_RAW=$(get_value "$BASELINE_FILE" ".tests.\"single-fetch\".response_time.max")
    BASELINE_SF_AVG_RAW=$(get_value "$BASELINE_FILE" ".tests.\"single-fetch\".response_time.avg")
    BASELINE_SF_P90_RAW=$(get_value "$BASELINE_FILE" ".tests.\"single-fetch\".response_time.p90")
    BASELINE_SF_P95_RAW=$(get_value "$BASELINE_FILE" ".tests.\"single-fetch\".response_time.p95")
    BASELINE_SF_P99_RAW=$(get_value "$BASELINE_FILE" ".tests.\"single-fetch\".response_time.p99")
    BASELINE_SF_RPS_RAW=$(get_value "$BASELINE_FILE" ".tests.\"single-fetch\".throughput.requests_per_second")

    BASELINE_DL_MIN_RAW=$(get_value "$BASELINE_FILE" ".tests.dataloader.response_time.min")
    BASELINE_DL_P50_RAW=$(get_value "$BASELINE_FILE" ".tests.dataloader.response_time.p50")
    BASELINE_DL_MAX_RAW=$(get_value "$BASELINE_FILE" ".tests.dataloader.response_time.max")
    BASELINE_DL_AVG_RAW=$(get_value "$BASELINE_FILE" ".tests.dataloader.response_time.avg")
    BASELINE_DL_P90_RAW=$(get_value "$BASELINE_FILE" ".tests.dataloader.response_time.p90")
    BASELINE_DL_P95_RAW=$(get_value "$BASELINE_FILE" ".tests.dataloader.response_time.p95")
    BASELINE_DL_P99_RAW=$(get_value "$BASELINE_FILE" ".tests.dataloader.response_time.p99")
    BASELINE_DL_RPS_RAW=$(get_value "$BASELINE_FILE" ".tests.dataloader.throughput.requests_per_second")

    # Format baseline values for display
    BASELINE_SF_MIN=$(format_number "$BASELINE_SF_MIN_RAW")
    BASELINE_SF_P50=$(format_number "$BASELINE_SF_P50_RAW")
    BASELINE_SF_MAX=$(format_number "$BASELINE_SF_MAX_RAW")
    BASELINE_SF_AVG=$(format_number "$BASELINE_SF_AVG_RAW")
    BASELINE_SF_P90=$(format_number "$BASELINE_SF_P90_RAW")
    BASELINE_SF_P95=$(format_number "$BASELINE_SF_P95_RAW")
    BASELINE_SF_P99=$(format_number "$BASELINE_SF_P99_RAW")
    BASELINE_SF_RPS=$(format_number "$BASELINE_SF_RPS_RAW")

    BASELINE_DL_MIN=$(format_number "$BASELINE_DL_MIN_RAW")
    BASELINE_DL_P50=$(format_number "$BASELINE_DL_P50_RAW")
    BASELINE_DL_MAX=$(format_number "$BASELINE_DL_MAX_RAW")
    BASELINE_DL_AVG=$(format_number "$BASELINE_DL_AVG_RAW")
    BASELINE_DL_P90=$(format_number "$BASELINE_DL_P90_RAW")
    BASELINE_DL_P95=$(format_number "$BASELINE_DL_P95_RAW")
    BASELINE_DL_P99=$(format_number "$BASELINE_DL_P99_RAW")
    BASELINE_DL_RPS=$(format_number "$BASELINE_DL_RPS_RAW")

    # Calculate changes using raw values
    CHANGE_SF_MIN=$(calc_change "$CURRENT_SF_MIN_RAW" "$BASELINE_SF_MIN_RAW")
    CHANGE_SF_P50=$(calc_change "$CURRENT_SF_P50_RAW" "$BASELINE_SF_P50_RAW")
    CHANGE_SF_MAX=$(calc_change "$CURRENT_SF_MAX_RAW" "$BASELINE_SF_MAX_RAW")
    CHANGE_SF_AVG=$(calc_change "$CURRENT_SF_AVG_RAW" "$BASELINE_SF_AVG_RAW")
    CHANGE_SF_P90=$(calc_change "$CURRENT_SF_P90_RAW" "$BASELINE_SF_P90_RAW")
    CHANGE_SF_P95=$(calc_change "$CURRENT_SF_P95_RAW" "$BASELINE_SF_P95_RAW")
    CHANGE_SF_P99=$(calc_change "$CURRENT_SF_P99_RAW" "$BASELINE_SF_P99_RAW")
    CHANGE_SF_RPS=$(calc_change "$CURRENT_SF_RPS_RAW" "$BASELINE_SF_RPS_RAW")

    CHANGE_DL_MIN=$(calc_change "$CURRENT_DL_MIN_RAW" "$BASELINE_DL_MIN_RAW")
    CHANGE_DL_P50=$(calc_change "$CURRENT_DL_P50_RAW" "$BASELINE_DL_P50_RAW")
    CHANGE_DL_MAX=$(calc_change "$CURRENT_DL_MAX_RAW" "$BASELINE_DL_MAX_RAW")
    CHANGE_DL_AVG=$(calc_change "$CURRENT_DL_AVG_RAW" "$BASELINE_DL_AVG_RAW")
    CHANGE_DL_P90=$(calc_change "$CURRENT_DL_P90_RAW" "$BASELINE_DL_P90_RAW")
    CHANGE_DL_P95=$(calc_change "$CURRENT_DL_P95_RAW" "$BASELINE_DL_P95_RAW")
    CHANGE_DL_P99=$(calc_change "$CURRENT_DL_P99_RAW" "$BASELINE_DL_P99_RAW")
    CHANGE_DL_RPS=$(calc_change "$CURRENT_DL_RPS_RAW" "$BASELINE_DL_RPS_RAW")

    # Add comparison section
    cat >> "$OUTPUT_MD" <<EOF
### 📊 Response Time

#### Current

| Test | Min | Med | Max | Avg | P90 | P95 | P99 |
|------|-----|-----|-----|-----|-----|-----|-----|
| **Single Fetch** | ${CURRENT_SF_MIN}ms | ${CURRENT_SF_P50}ms | ${CURRENT_SF_MAX}ms | ${CURRENT_SF_AVG}ms | ${CURRENT_SF_P90}ms | ${CURRENT_SF_P95}ms | ${CURRENT_SF_P99}ms |
| **DataLoader** | ${CURRENT_DL_MIN}ms | ${CURRENT_DL_P50}ms | ${CURRENT_DL_MAX}ms | ${CURRENT_DL_AVG}ms | ${CURRENT_DL_P90}ms | ${CURRENT_DL_P95}ms | ${CURRENT_DL_P99}ms |

#### Baseline

| Test | Min | Med | Max | Avg | P90 | P95 | P99 |
|------|-----|-----|-----|-----|-----|-----|-----|
| **Single Fetch** | ${BASELINE_SF_MIN}ms | ${BASELINE_SF_P50}ms | ${BASELINE_SF_MAX}ms | ${BASELINE_SF_AVG}ms | ${BASELINE_SF_P90}ms | ${BASELINE_SF_P95}ms | ${BASELINE_SF_P99}ms |
| **DataLoader** | ${BASELINE_DL_MIN}ms | ${BASELINE_DL_P50}ms | ${BASELINE_DL_MAX}ms | ${BASELINE_DL_AVG}ms | ${BASELINE_DL_P90}ms | ${BASELINE_DL_P95}ms | ${BASELINE_DL_P99}ms |

#### Change vs Baseline

| Test | Min | Med | Max | Avg | P90 | P95 | P99 |
|------|-----|-----|-----|-----|-----|-----|-----|
| **Single Fetch** | $(get_emoji "$CHANGE_SF_MIN" "latency") $(format_change "$CHANGE_SF_MIN" "latency") | $(get_emoji "$CHANGE_SF_P50" "latency") $(format_change "$CHANGE_SF_P50" "latency") | $(get_emoji "$CHANGE_SF_MAX" "latency") $(format_change "$CHANGE_SF_MAX" "latency") | $(get_emoji "$CHANGE_SF_AVG" "latency") $(format_change "$CHANGE_SF_AVG" "latency") | $(get_emoji "$CHANGE_SF_P90" "latency") $(format_change "$CHANGE_SF_P90" "latency") | $(get_emoji "$CHANGE_SF_P95" "latency") $(format_change "$CHANGE_SF_P95" "latency") | $(get_emoji "$CHANGE_SF_P99" "latency") $(format_change "$CHANGE_SF_P99" "latency") |
| **DataLoader** | $(get_emoji "$CHANGE_DL_MIN" "latency") $(format_change "$CHANGE_DL_MIN" "latency") | $(get_emoji "$CHANGE_DL_P50" "latency") $(format_change "$CHANGE_DL_P50" "latency") | $(get_emoji "$CHANGE_DL_MAX" "latency") $(format_change "$CHANGE_DL_MAX" "latency") | $(get_emoji "$CHANGE_DL_AVG" "latency") $(format_change "$CHANGE_DL_AVG" "latency") | $(get_emoji "$CHANGE_DL_P90" "latency") $(format_change "$CHANGE_DL_P90" "latency") | $(get_emoji "$CHANGE_DL_P95" "latency") $(format_change "$CHANGE_DL_P95" "latency") | $(get_emoji "$CHANGE_DL_P99" "latency") $(format_change "$CHANGE_DL_P99" "latency") |

### ⚡ Throughput

| Test | Metric | Current | Baseline | Change |
|------|--------|---------|----------|--------|
| **Single Fetch** | **Requests/sec** | ${CURRENT_SF_RPS} req/s | ${BASELINE_SF_RPS} req/s | $(get_emoji "$CHANGE_SF_RPS" "throughput") $(format_change "$CHANGE_SF_RPS" "throughput") |
| **DataLoader** | **Requests/sec** | ${CURRENT_DL_RPS} req/s | ${BASELINE_DL_RPS} req/s | $(get_emoji "$CHANGE_DL_RPS" "throughput") $(format_change "$CHANGE_DL_RPS" "throughput") |

### 🎯 Reliability

| Test | Error Rate |
|------|------------|
| **Single Fetch** | ${CURRENT_SF_ERR}% $(get_emoji "0" "error") |
| **DataLoader** | ${CURRENT_DL_ERR}% $(get_emoji "0" "error") |

### 🔍 Analysis

EOF

    # Determine overall assessment
    SIGNIFICANT_REGRESSION=false

    for change in "$CHANGE_SF_P95" "$CHANGE_DL_P95"; do
        abs_change=$(echo "$change" | tr -d '-')
        if (( $(echo "$abs_change > 10" | bc -l) )) && (( $(echo "$change > 0" | bc -l) )); then
            SIGNIFICANT_REGRESSION=true
            break
        fi
    done

    if [ "$SIGNIFICANT_REGRESSION" == "true" ]; then
        echo "⚠️ **Performance regression detected.** Response times have increased significantly compared to baseline." >> "$OUTPUT_MD"
    else
        echo "✅ No significant performance regression detected" >> "$OUTPUT_MD"
    fi

else
    # No baseline - just show current metrics
    cat >> "$OUTPUT_MD" <<EOF
### 📊 Response Time

| Test | Min | Med | Max | Avg | P90 | P95 | P99 |
|------|-----|-----|-----|-----|-----|-----|-----|
| **Single Fetch** | ${CURRENT_SF_MIN}ms | ${CURRENT_SF_P50}ms | ${CURRENT_SF_MAX}ms | ${CURRENT_SF_AVG}ms | ${CURRENT_SF_P90}ms | ${CURRENT_SF_P95}ms | ${CURRENT_SF_P99}ms |
| **DataLoader** | ${CURRENT_DL_MIN}ms | ${CURRENT_DL_P50}ms | ${CURRENT_DL_MAX}ms | ${CURRENT_DL_AVG}ms | ${CURRENT_DL_P90}ms | ${CURRENT_DL_P95}ms | ${CURRENT_DL_P99}ms |

### ⚡ Throughput

| Test | Requests/sec |
|------|-------------|
| **Single Fetch** | ${CURRENT_SF_RPS} req/s |
| **DataLoader** | ${CURRENT_DL_RPS} req/s |

### 🎯 Reliability

| Test | Error Rate |
|------|------------|
| **Single Fetch** | ${CURRENT_SF_ERR}% |
| **DataLoader** | ${CURRENT_DL_ERR}% |

_No baseline data available for comparison._

EOF
fi

echo "Performance report generated: $OUTPUT_MD"
cat "$OUTPUT_MD"
