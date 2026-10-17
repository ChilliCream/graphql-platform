#!/usr/bin/env bash
set -euo pipefail

# Verify CPU affinity of benchmark processes.
# Usage: verify-pinning.sh <gateway-pid> <subgraph-pid>

if ! command -v taskset &>/dev/null; then
  echo "Pinning verification: skipped (no taskset)"
  exit 0
fi

echo "=== CPU Affinity Verification ==="

# Check affinity of a process and its first child (the taskset'd process)
check_affinity() {
  local label="$1"
  local pid="$2"
  if ! kill -0 "$pid" 2>/dev/null; then
    echo "  ${label} (PID $pid): not running"
    return 0
  fi

  # The captured PID is the outer subshell; the actual pinned process is a child
  local child
  child=$(pgrep -P "$pid" 2>/dev/null | head -1 || true)
  local check_pid="${child:-$pid}"

  if ! kill -0 "$check_pid" 2>/dev/null; then
    echo "  ${label} (PID $check_pid): not running"
    return 0
  fi

  local affinity
  affinity=$(taskset -cp "$check_pid" 2>/dev/null | awk -F': ' '{print $2}' || true)
  if [[ -n "$affinity" ]]; then
    echo "  ${label} (PID $check_pid): $affinity"
  else
    echo "  ${label} (PID $check_pid): affinity unavailable"
  fi
}

GATEWAY_PID="${1:-}"
SUBGRAPH_PID="${2:-}"

if [[ -n "$GATEWAY_PID" ]]; then
  check_affinity "Gateway" "$GATEWAY_PID"
fi

if [[ -n "$SUBGRAPH_PID" ]]; then
  check_affinity "Subgraph" "$SUBGRAPH_PID"
fi

# Check for stray processes on benchmark cores
echo "  Checking for stray processes on benchmark cores..."
STRAY=$(ps -eo pid,psr,comm --sort=-psr 2>/dev/null \
  | awk '$2 >= 1 && $2 <= 7 && $3 !~ /^(migration|watchdog|ksoftirq|kworker|rcu_)/ {print}' \
  | head -5 || true)
if [[ -n "$STRAY" ]]; then
  echo "  ⚠️  Found processes on benchmark cores:"
  echo "$STRAY" | sed 's/^/    /'
else
  echo "  ✅ No stray userspace processes on benchmark cores"
fi
