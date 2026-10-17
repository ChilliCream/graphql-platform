#!/usr/bin/env bash
set -Eeuo pipefail

# Samples CPU, RSS, and k6 metrics for an entire process tree.
# Usage: ./monitor.sh [-g <pgid> | -p <pid>] [-o data.csv] [-i 1] [-k k6_api_addr] [-K k6_cpuset] [-S subgraph_cpuset] [-G gateway_cpuset]
#   -g PGID        Process group id to monitor
#   -p PID         Leader process id to monitor
#   -o OUTPUT      CSV file (default: data.csv)
#   -i INTERVAL    Seconds between samples (default: 1)
#   -k K6_API      k6 REST API address (e.g., 127.0.0.1:6565)
#   -K K6_CPUSET   k6 CPU core set (e.g., "1,9")
#   -S SUB_CPUSET  subgraph CPU core set (e.g., "4-7,12-15")
#   -G GW_CPUSET   gateway CPU core set (e.g., "2,3,10,11")

PGID=""
LEADER_PID=""
OUTPUT_FILE="data.csv"
INTERVAL="1"
K6_API=""
K6_CPUSET=""
SUBGRAPH_CPUSET=""
GATEWAY_CPUSET=""

while getopts ":g:p:o:i:k:K:S:G:" opt; do
  case "$opt" in
    g) PGID="$OPTARG" ;;
    p) LEADER_PID="$OPTARG" ;;
    o) OUTPUT_FILE="$OPTARG" ;;
    i) INTERVAL="$OPTARG" ;;
    k) K6_API="$OPTARG" ;;
    K) K6_CPUSET="$OPTARG" ;;
    S) SUBGRAPH_CPUSET="$OPTARG" ;;
    G) GATEWAY_CPUSET="$OPTARG" ;;
    *) echo "Usage: $0 [-g <pgid> | -p <pid>] [-o output.csv] [-i interval] [-k k6_api] [-K k6_cpuset] [-S subgraph_cpuset] [-G gateway_cpuset]"; exit 1 ;;
  esac
done

if [[ -z "$PGID" && -z "$LEADER_PID" ]]; then
  echo "Error: Either -g <pgid> or -p <pid> is required."
  echo "Usage: $0 [-g <pgid> | -p <pid>] [-o output.csv] [-i interval]"
  exit 1
fi

if [[ -n "$PGID" ]]; then
    echo "Monitoring PGID $PGID. Writing to $OUTPUT_FILE. Ctrl+C to stop."
else
    echo "Monitoring PID tree $LEADER_PID. Writing to $OUTPUT_FILE. Ctrl+C to stop."
fi

echo "Seconds,VUs,RPS,P95_ms,Req_success_rate,Total_CPU,K6_Core_CPU,Subgraph_Core_CPU,Gateway_Core_CPU,Total_RSS_KB" > "$OUTPUT_FILE"

list_descendant_pids() {
  local root_pid="$1"
  local queue=("$root_pid")
  local current
  local child

  while [[ ${#queue[@]} -gt 0 ]]; do
    current="${queue[0]}"
    queue=("${queue[@]:1}")
    while IFS= read -r child; do
      [[ -z "$child" ]] && continue
      echo "$child"
      queue+=("$child")
    done < <(pgrep -P "$current" 2>/dev/null || true)
  done
}

get_target_pids() {
  local pids=""
  if [[ -n "$PGID" ]]; then
    pids=$(pgrep -g "$PGID" || true)
  elif [[ -n "$LEADER_PID" ]]; then
    pids="$LEADER_PID"
    while IFS= read -r pid; do
      [[ -n "$pid" ]] && pids="$pids $pid"
    done < <(list_descendant_pids "$LEADER_PID")
  fi
  echo "$pids" | tr '\n' ' '
}

get_k6_metrics() {
    if [[ -z "$K6_API" ]]; then
        printf "0,0.00,0.00,0.00\n"
        return
    fi

    local metrics_json
    metrics_json=$(curl -fsS "http://$K6_API/v1/metrics" 2>/dev/null || echo "{}")

    local result
    result=$(echo "$metrics_json" | jq -r '
        ((.data // []) | map({key: .id, value: .}) | from_entries) as $metrics |
        ($metrics.vus.attributes.sample.value // 0) as $vus |
        ($metrics.http_reqs.attributes.sample.rate // 0) as $rps |
        ($metrics.http_req_duration.attributes.sample."p(95)" // 0) as $p95 |
        ($metrics.success_rate.attributes.sample.rate // 1) as $req_success_rate |
        "\($vus),\($rps),\($p95),\($req_success_rate)"
    ' || printf "0,0.00,0.00,0.00\n")

    if [[ -z "$result" ]]; then
        printf "0,0.00,0.00,0.00\n"
        return
    fi
    echo "$result"
}

sum_cpu_rss() {
  local pids="$1"
  if [[ -z "$pids" ]]; then
    printf "0.00,0\n"
    return
  fi
  local ps_args=()
  for pid in $pids; do
    ps_args+=(-p "$pid")
  done

  local ps_output
  if ! ps_output=$(ps "${ps_args[@]}" -o %cpu=,rss= 2>/dev/null); then
    printf "0.00,0\n"
    return
  fi

  awk '
    NF >= 2 { cpu+=$1; rss+=$2; seen=1 }
    END {
      if (seen) {
        printf("%.2f,%d\n", cpu, rss)
      } else {
        printf("0.00,0\n")
      }
    }
  ' <<< "$ps_output"
}

expand_cpuset() {
  local cpuset="$1"
  python3 - "$cpuset" <<'PY'
import sys
cpuset = (sys.argv[1] or "").strip()
if not cpuset:
    sys.exit(0)
cores = []
for part in cpuset.split(","):
    part = part.strip()
    if not part:
        continue
    if "-" in part:
        lo, hi = part.split("-", 1)
        lo_i, hi_i = int(lo), int(hi)
        if lo_i <= hi_i:
            cores.extend(range(lo_i, hi_i + 1))
        else:
            cores.extend(range(hi_i, lo_i + 1))
    else:
        cores.append(int(part))
print(" ".join(str(c) for c in sorted(set(cores))))
PY
}

declare -A PREV_TOTAL=()
declare -A PREV_IDLE=()
declare -A CUR_TOTAL=()
declare -A CUR_IDLE=()
declare -A CORE_SET=()
ALL_CORES=()
K6_CORES=()
SUBGRAPH_CORES=()
GATEWAY_CORES=()

read -r -a K6_CORES <<< "$(expand_cpuset "$K6_CPUSET")"
read -r -a SUBGRAPH_CORES <<< "$(expand_cpuset "$SUBGRAPH_CPUSET")"
read -r -a GATEWAY_CORES <<< "$(expand_cpuset "$GATEWAY_CPUSET")"

for core in "${K6_CORES[@]}" "${SUBGRAPH_CORES[@]}" "${GATEWAY_CORES[@]}"; do
  [[ -z "${core:-}" ]] && continue
  if [[ -z "${CORE_SET[$core]:-}" ]]; then
    CORE_SET["$core"]=1
    ALL_CORES+=("$core")
  fi
done

capture_core_snapshot() {
  local core line user nice system idle iowait irq softirq steal guest guest_nice total idle_all
  for core in "${ALL_CORES[@]}"; do
    line="$(grep -E "^cpu${core} " /proc/stat 2>/dev/null || true)"
    [[ -z "$line" ]] && continue
    read -r _ user nice system idle iowait irq softirq steal guest guest_nice <<< "$line"
    total=$((user + nice + system + idle + iowait + irq + softirq + steal))
    idle_all=$((idle + iowait))
    CUR_TOTAL["$core"]="$total"
    CUR_IDLE["$core"]="$idle_all"
  done
}

group_avg_usage() {
  local -n group_ref="$1"
  local sum="0"
  local count=0
  local core prev_total prev_idle cur_total cur_idle dt di usage

  for core in "${group_ref[@]}"; do
    prev_total="${PREV_TOTAL[$core]:-}"
    prev_idle="${PREV_IDLE[$core]:-}"
    cur_total="${CUR_TOTAL[$core]:-}"
    cur_idle="${CUR_IDLE[$core]:-}"
    if [[ -z "$prev_total" || -z "$prev_idle" || -z "$cur_total" || -z "$cur_idle" ]]; then
      continue
    fi

    dt=$((cur_total - prev_total))
    di=$((cur_idle - prev_idle))
    if (( dt <= 0 )); then
      continue
    fi

    usage="$(awk -v total="$dt" -v idle_delta="$di" '
      BEGIN {
        u = ((total-idle_delta)/total)*100.0
        if (u < 0) u = 0
        if (u > 100) u = 100
        printf "%.4f", u
      }'
    )"
    sum="$(awk -v a="$sum" -v b="$usage" 'BEGIN { printf "%.6f", a+b }')"
    count=$((count + 1))
  done

  if (( count == 0 )); then
    printf "0.00"
  else
    awk -v s="$sum" -v n="$count" '
      BEGIN {
        avg = s/n
        if (avg < 0) avg = 0
        if (avg > 100) avg = 100
        printf "%.2f", avg
      }'
  fi
}

save_prev_snapshot() {
  local core
  for core in "${ALL_CORES[@]}"; do
    PREV_TOTAL["$core"]="${CUR_TOTAL[$core]:-}"
    PREV_IDLE["$core"]="${CUR_IDLE[$core]:-}"
  done
}

# Prime CPU snapshot state so first emitted row has valid deltas.
if [[ -f /proc/stat && ${#ALL_CORES[@]} -gt 0 ]]; then
  capture_core_snapshot
  save_prev_snapshot
fi

START_TIME=$(date +%s)
while true; do
  PIDS="$(get_target_pids)"
  if [[ -z "$PIDS" ]]; then
    if [[ -n "$PGID" ]]; then
        echo "Process group $PGID is empty. Stopping monitoring."
    else
        echo "Process tree for $LEADER_PID is empty. Stopping monitoring."
    fi
    break
  fi

  k6_metrics="$(get_k6_metrics)"
  IFS=',' read -r VUS RPS P95_MS REQ_SUCCESS_RATE <<< "$k6_metrics"

  CPU_RSS="$(sum_cpu_rss "$PIDS")"
  CPU="$(echo "$CPU_RSS" | cut -d, -f1)"
  RSS_KB="$(echo "$CPU_RSS" | cut -d, -f2)"
  K6_CORE_CPU="0.00"
  SUBGRAPH_CORE_CPU="0.00"
  GATEWAY_CORE_CPU="0.00"

  if [[ -f /proc/stat && ${#ALL_CORES[@]} -gt 0 ]]; then
    capture_core_snapshot
    K6_CORE_CPU="$(group_avg_usage K6_CORES)"
    SUBGRAPH_CORE_CPU="$(group_avg_usage SUBGRAPH_CORES)"
    GATEWAY_CORE_CPU="$(group_avg_usage GATEWAY_CORES)"
    save_prev_snapshot
  fi

  if [[ $VUS -gt 0 ]]; then
      NOW=$(date +%s)
      ELAPSED=$((NOW - START_TIME))
      echo "$ELAPSED,$VUS,$RPS,$P95_MS,$REQ_SUCCESS_RATE,$CPU,$K6_CORE_CPU,$SUBGRAPH_CORE_CPU,$GATEWAY_CORE_CPU,$RSS_KB" >> "$OUTPUT_FILE"
  fi

  sleep "$INTERVAL"
done
