#!/usr/bin/env bash
# Extract the final completed-state frame from a VHS .txt capture.
#
#   extract-frame.sh <capture.txt> [marker]
#
# `Output foo.txt` in a tape concatenates every rendered frame, separated by a
# horizontal rule (a line of box-drawing dashes). For a stable golden we keep
# only the last frame that reached the flow's success marker, with trailing
# whitespace and surrounding blank lines trimmed.
#
# `marker` is a literal substring the completed frame must contain (e.g. "Done!"
# for an install, "Successfully removed" for a removal). When omitted/empty the
# last non-blank frame is used.
#
# Exit non-zero (instead of silently falling back) when the capture is missing or
# empty, or when a non-empty marker is never seen. This makes a broken recording
# or a wrong/typo'd marker fail loudly rather than (a) passing on the wrong frame
# or (b) committing an empty golden via `run.sh --update`.
set -euo pipefail

TXT="${1:-/dev/stdin}"
MARKER="${2:-}"

if [[ "$TXT" != "/dev/stdin" && ! -s "$TXT" ]]; then
  echo "extract-frame: capture is missing or empty: $TXT" >&2
  exit 1
fi

awk -v marker="$MARKER" '
  function store() {
    if (buf ~ /[^[:space:]]/) {
      anylast = buf                                   # last non-blank frame (fallback)
      if (marker == "" || index(buf, marker) > 0) {
        last = buf                                    # last frame matching the marker
        seen = 1
      }
    }
    buf = ""
  }
  /^────/ { store(); next }
  { line = $0; sub(/[ \t\r]+$/, "", line); buf = buf line "\n" }
  END {
    store()
    # A required marker that never appeared means the flow did not reach its
    # success state (or the marker is wrong): fail rather than emit a stale frame.
    if (marker != "" && !seen) {
      print "extract-frame: success marker never appeared: " marker > "/dev/stderr"
      exit 3
    }
    out = (last != "" ? last : anylast)
    sub(/^\n+/, "", out)
    sub(/\n+$/, "\n", out)
    printf "%s", out
  }
' "$TXT"
