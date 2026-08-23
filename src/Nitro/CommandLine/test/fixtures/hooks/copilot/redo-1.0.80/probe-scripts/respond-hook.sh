#!/usr/bin/env bash
# Reads stdin JSON, saves it, and prints S5_RESPOND_JSON as the hook's stdout response.
set -u
CAPTURE_DIR="${S5_CAPTURE_DIR:-/tmp/s5-captures}"
TAG="${S5_TAG:-untagged}"
mkdir -p "$CAPTURE_DIR"
cat > "$CAPTURE_DIR/${TAG}.input.json"
printf '%s' "$S5_RESPOND_JSON" > "$CAPTURE_DIR/${TAG}.response-sent.json"
printf '%s' "$S5_RESPOND_JSON"
exit 0
