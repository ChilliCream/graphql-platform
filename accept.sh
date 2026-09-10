#!/usr/bin/env bash
#
# Promote snapshot files left in `__mismatch__/` directories by failing snapshot
# tests to be the new canonical snapshots.
#
# For each `__mismatch__/` directory under the repo root:
#   - move its top-level files up one level (overwriting any existing snapshot),
#   - delete everything else inside it (nested mismatches), and
#   - remove the `__mismatch__/` directory itself.

set -euo pipefail
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

while IFS= read -r -d '' mismatch_dir; do
  echo "Analyzing $mismatch_dir ..."
  snapshot_dir=$(dirname "$mismatch_dir")

  while IFS= read -r -d '' file; do
    snapshot_file="$snapshot_dir/$(basename "$file")"
    rm -f "$snapshot_file"
    mv "$file" "$snapshot_file"
  done < <(find "$mismatch_dir" -maxdepth 1 -type f -print0)

  rm -rf "$mismatch_dir"
done < <(find "$SCRIPT_DIR" -type d -name "__mismatch__" -print0)
