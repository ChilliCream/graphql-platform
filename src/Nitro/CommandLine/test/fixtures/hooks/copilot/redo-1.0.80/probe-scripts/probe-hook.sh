#!/usr/bin/env bash
# Generic probe hook: reads stdin JSON, writes it to CAPTURE_DIR/<TAG>.json,
# and optionally echoes a canned response JSON if RESPOND_JSON is set.
set -u
CAPTURE_DIR="${S5_CAPTURE_DIR:-/tmp/s5-captures}"
TAG="${S5_TAG:-untagged}"
mkdir -p "$CAPTURE_DIR"
cat > "$CAPTURE_DIR/${TAG}.json"
if [ -n "${S5_RESPOND_JSON:-}" ]; then
  printf '%s' "$S5_RESPOND_JSON"
fi
exit 0
