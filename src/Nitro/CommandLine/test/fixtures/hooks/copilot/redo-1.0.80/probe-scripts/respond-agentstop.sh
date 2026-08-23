#!/usr/bin/env bash
set -u
CAPTURE_DIR="$S5_CAPTURE_DIR"
TAG="$S5_TAG"
GUARD="$S5_GUARD"
mkdir -p "$CAPTURE_DIR"
cat > "$CAPTURE_DIR/${TAG}.$(date +%s%N).json"
if [ ! -f "$GUARD" ]; then
  touch "$GUARD"
  printf '%s' '{"decision":"block","reason":"s5-spike-block-once-test"}'
else
  printf '%s' '{"decision":"approve"}'
fi
exit 0
