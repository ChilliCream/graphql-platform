#!/usr/bin/env bash
set -euo pipefail

# Confine all existing processes to the specified CPU core(s).
# Usage: ./isolate-system-cpus.sh <cpuset>
#   e.g. ./isolate-system-cpus.sh 0

CPUSET="${1:?Usage: isolate-system-cpus.sh <cpuset>}"

# Only works on Linux with cgroups v2
if [[ "$(uname -s)" != "Linux" ]]; then
  echo "System CPU isolation: skipped (not Linux)"
  exit 0
fi

if [[ ! -d /sys/fs/cgroup ]]; then
  echo "System CPU isolation: skipped (cgroups not available)"
  exit 0
fi

echo "Isolating system processes to core(s): $CPUSET"

if ! sudo -n mkdir -p /sys/fs/cgroup/system_tasks 2>/dev/null; then
  echo "System CPU isolation: skipped (sudo not permitted for cgroup operations)"
  exit 0
fi
echo "$CPUSET" | sudo tee /sys/fs/cgroup/system_tasks/cpuset.cpus > /dev/null
echo "0" | sudo tee /sys/fs/cgroup/system_tasks/cpuset.mems > /dev/null

MOVED=0
FAILED=0
for pid in $(ps -eo pid --no-headers); do
  if sudo sh -c "echo $pid > /sys/fs/cgroup/system_tasks/cgroup.procs" 2>/dev/null; then
    MOVED=$((MOVED + 1))
  else
    FAILED=$((FAILED + 1))
  fi
done

echo "System CPU isolation: moved $MOVED processes, $FAILED unmovable (kernel threads — expected)"
