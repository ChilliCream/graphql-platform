#!/usr/bin/env bash
# Record the nitro CLI flows with VHS and verify each against its golden frame.
#
#   ./run.sh                  record every flow, diff each final frame vs its golden (CI / test)
#   ./run.sh --update         record every flow, overwrite goldens + committed GIFs (accept output)
#   ./run.sh remove update    record only the named flows (any subset of the list below)
#   ./run.sh --update init    update a single flow
#
# Each flow <name> is driven by test/e2e/<name>-flow.tape and checked against
# test/e2e/<name>-flow.golden.txt. One VHS run per flow produces both the demo
# GIF (<name>-flow.gif) and the text capture that is reduced to a deterministic
# final frame and diffed.
#
# Inputs (the repo) are mounted read-only into the pinned VHS container; the
# container bundles ttyd + ffmpeg + fonts so nothing needs installing on the host.
# The published binary and raw recordings land in gitignored dirs (bin/, out/).
#
# Results for changed/new flows are collected under out/report/ for CI to upload
# as artifacts and surface in a PR comment:
#   out/report/status.tsv      <flow>\t<PASS|FAIL|NEW>   (every recorded flow)
#   out/report/changed.txt     one <flow> per line       (FAIL or NEW only)
#   out/report/<flow>-flow.gif / .frame.txt / .golden.txt / .diff
#
# Exit code: 0 if every recorded flow PASSed; 1 if any FAILed or was NEW (no
# golden yet); 2 on usage/setup error; 3 if docker is unavailable. --update
# exits 0 when every targeted flow recorded successfully, or 1 if any recording
# failed (its golden is left unchanged).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../../.." && pwd)"
OUT_DIR="$SCRIPT_DIR/out"
BIN_DIR="$SCRIPT_DIR/bin"
REPORT_DIR="$OUT_DIR/report"

# Pinned by digest so a new VHS release can't silently shift the rendered output
# (and thus the goldens). Bump deliberately, then re-run with --update. Tag: v0.11.0.
VHS_IMAGE="${VHS_IMAGE:-ghcr.io/charmbracelet/vhs@sha256:9d5fc3dc0c160b0fb1d2212baff07e6bdf3fa9438c504a3237484567302fcf93}"

# Every flow, in record order, with the literal marker the completed final frame
# must contain (see extract-frame.sh). Keep this list and ALL_FLOWS in sync.
#
# `help` is a trivial pipeline smoke flow: it proves publish -> record -> extract
# -> diff works end to end before fixture-backed task flows land. The rest run
# against the deterministic fixture prepared below (out/fixture/acme).
declare -A MARKERS=(
  [help]="List tasks."
  [init]="Initialized task workspace"
  [list]="acme-e5f"
  [show]="Draft new pricing copy"
  [create]="Created task '"
  [close-reopen]="Reopened task 'acme-a1b'."
  [dep-tree]="acme-epic1.1"
  [error]="[nitro exit: 1]"
)
ALL_FLOWS=(help init list show create close-reopen dep-tree error)

# Per-flow sed expressions (extended regex, `sed -E`) applied to the extracted
# frame before it is compared with (or written as) the golden. Empty by default.
#
# `create` mints its task ID from the title and wall-clock time
# (TaskStore.CreateTaskIdAsync), so it is non-deterministic across recordings;
# normalize it to a fixed placeholder before diffing. The expression is a
# single literal substitution (no alternation/backreferences) so it cannot
# fail on a well-formed frame.
declare -A SCRUBS=(
  [create]='s/acme-[a-z0-9.]+/acme-XXX/g'
)

# --- args: optional --update plus an optional subset of flow names ------------
UPDATE=0
FLOWS=()
for arg in "$@"; do
  case "$arg" in
    --update) UPDATE=1 ;;
    -*) echo "unknown option: $arg" >&2; exit 2 ;;
    *)
      if [[ -z "${MARKERS[$arg]+x}" ]]; then
        echo "unknown flow: $arg (known: ${ALL_FLOWS[*]})" >&2
        exit 2
      fi
      FLOWS+=("$arg")
      ;;
  esac
done
[[ ${#FLOWS[@]} -eq 0 ]] && FLOWS=("${ALL_FLOWS[@]}")

if ! command -v docker >/dev/null 2>&1; then
  echo "docker is required to run the VHS recordings" >&2
  exit 3
fi

# 1. Publish a self-contained linux-x64 binary (glibc, matches the Debian-based
#    VHS image). Skip if already present unless REBUILD=1 or --update, an update
#    must never bake a stale binary's output into the committed goldens.
#
#    The csproj sets UseAppHost=false (it is packed as a dotnet tool), so a
#    non-AOT e2e publish must override UseAppHost=true to get a native `nitro`
#    executable rather than only `nitro.dll`. Task commands need no auth, so no
#    Nitro*ClientId secret properties are passed; the BuildSecrets constants
#    default to empty.
#
#    NITRO_E2E_AOT=1 publishes with PublishAot=true instead, matching the
#    shipped binary. This is slow, so it is off by default.
if [[ ! -x "$BIN_DIR/nitro" || "${REBUILD:-0}" == "1" || "$UPDATE" == "1" ]]; then
  echo "==> publishing nitro (linux-x64, self-contained)"
  aot_flag=false
  [[ "${NITRO_E2E_AOT:-0}" == "1" ]] && aot_flag=true
  dotnet publish "$REPO_ROOT/src/Nitro/CommandLine/src/CommandLine" \
    -c Release -f net11.0 -r linux-x64 -o "$BIN_DIR" --self-contained \
    -p:TargetFrameworks=net11.0 -p:RuntimeIdentifiers=linux-x64 \
    -p:PublishAot=$aot_flag -p:UseAppHost=true >/dev/null
fi

mkdir -p "$OUT_DIR"
rm -rf "$REPORT_DIR"
mkdir -p "$REPORT_DIR"
: > "$REPORT_DIR/status.tsv"
: > "$REPORT_DIR/changed.txt"

# 1b. Prepare a deterministic fixture task workspace (fixed IDs, timestamps,
#     actors: see fixtures/README.md) that tapes `cp -r` into their own
#     throwaway /tmp/work rather than seeding state live inside a Hide block.
#     Rebuilt unconditionally on every run (not cached like the binary) so
#     schema drift between TaskStoreSchema and fixtures/seed.sql fails fast
#     here, with a message pointing at fixtures/README.md, instead of turning
#     into a confusing golden diff once a task flow tape lands.
if ! command -v sqlite3 >/dev/null 2>&1; then
  echo "sqlite3 is required to prepare the fixture workspace" >&2
  exit 3
fi

FIXTURE_DIR="$OUT_DIR/fixture/acme"
# Matches TaskWorkspace.RootDirectoryName/TasksDirectoryName/DatabaseFileName.
FIXTURE_DB="$FIXTURE_DIR/.nitro/tasks/tasks.db"
FIXTURE_MARKER="acme-epic1"

echo "==> preparing fixture workspace (out/fixture/acme)"
rm -rf "$FIXTURE_DIR"
mkdir -p "$FIXTURE_DIR"
if ! ( cd "$FIXTURE_DIR" && NITRO_TASK_ACTOR=e2e-agent "$BIN_DIR/nitro" task init >/dev/null ); then
  echo "==> fixture prepare FAILED: 'nitro task init' did not succeed" >&2
  exit 2
fi
if ! sqlite3 "$FIXTURE_DB" < "$SCRIPT_DIR/fixtures/seed.sql"; then
  echo "==> fixture prepare FAILED: seed.sql did not apply cleanly to $FIXTURE_DB" >&2
  echo "    the task schema likely drifted from fixtures/seed.sql; see fixtures/README.md" >&2
  exit 2
fi
if ! ( cd "$FIXTURE_DIR" && "$BIN_DIR/nitro" task list ) | grep -q "$FIXTURE_MARKER"; then
  echo "==> fixture guard FAILED: 'nitro task list' did not show '$FIXTURE_MARKER'" >&2
  echo "    the task schema likely drifted from fixtures/seed.sql; see fixtures/README.md" >&2
  exit 2
fi
echo "    fixture ready, guard passed"

overall=0
declare -a SUMMARY=()

for flow in "${FLOWS[@]}"; do
  tape="$SCRIPT_DIR/${flow}-flow.tape"
  golden="$SCRIPT_DIR/${flow}-flow.golden.txt"
  frame="$OUT_DIR/${flow}-flow.frame.txt"

  if [[ ! -f "$tape" ]]; then
    echo "==> $flow: missing tape ($tape)" >&2
    exit 2
  fi

  # 2. Record. The tape writes <flow>-flow.gif + <flow>-flow.txt into OUT_DIR
  #    (mounted as the VHS workdir at /vhs). A single flow's failure (docker/VHS
  #    error, or a Wait sentinel that times out) must NOT abort the batch under
  #    `set -e`: capture it, record the flow as FAIL, and carry on to the rest.
  echo "==> recording ${flow}-flow"
  rec_ok=1
  if ! docker run --rm --shm-size=512m \
      -v "$REPO_ROOT":/src:ro \
      -v "$OUT_DIR":/vhs \
      "$VHS_IMAGE" "/src/src/Nitro/CommandLine/test/e2e/${flow}-flow.tape"; then
    rec_ok=0
    echo "    recording FAILED (docker/VHS error)" >&2
  fi

  # 3. Reduce the multi-frame capture to the deterministic final frame.
  #    extract-frame.sh exits non-zero if the capture is missing/empty or the
  #    success marker never appeared; treat that as a failed recording too.
  extract_ok=0
  if [[ "$rec_ok" == "1" ]] \
      && bash "$SCRIPT_DIR/extract-frame.sh" "$OUT_DIR/${flow}-flow.txt" "${MARKERS[$flow]}" > "$frame"; then
    extract_ok=1
  else
    : > "$frame"
    echo "    frame extraction FAILED (empty capture or missing marker '${MARKERS[$flow]}')" >&2
  fi
  recorded_ok=0
  if [[ "$extract_ok" == "1" && -s "$frame" ]]; then
    recorded_ok=1
  fi

  # 3b. Normalize non-deterministic content (task IDs, dates) before compare/update.
  if [[ "$recorded_ok" == "1" && -n "${SCRUBS[$flow]:-}" ]]; then
    sed -E -i "${SCRUBS[$flow]}" "$frame"
  fi

  # 4. Update or verify.
  if [[ "$UPDATE" == "1" ]]; then
    if [[ "$recorded_ok" == "1" ]]; then
      cp "$frame" "$golden"
      cp "$OUT_DIR/${flow}-flow.gif" "$SCRIPT_DIR/${flow}-flow.gif"
      echo "    updated golden + GIF"
      SUMMARY+=("$flow UPDATED")
    else
      overall=1
      echo "    NOT updated, recording failed, golden left unchanged" >&2
      SUMMARY+=("$flow FAILED")
    fi
    continue
  fi

  if [[ "$recorded_ok" != "1" ]]; then
    status=FAIL
  elif [[ ! -f "$golden" ]]; then
    status=NEW
  elif diff -u "$golden" "$frame" > "$REPORT_DIR/${flow}-flow.diff" 2>&1; then
    status=PASS
    rm -f "$REPORT_DIR/${flow}-flow.diff"
  else
    status=FAIL
  fi

  printf '%s\t%s\n' "$flow" "$status" >> "$REPORT_DIR/status.tsv"
  SUMMARY+=("$flow $status")

  if [[ "$status" != "PASS" ]]; then
    overall=1
    echo "$flow" >> "$REPORT_DIR/changed.txt"
    cp -f "$OUT_DIR/${flow}-flow.gif" "$REPORT_DIR/${flow}-flow.gif" 2>/dev/null || true
    cp -f "$frame" "$REPORT_DIR/${flow}-flow.frame.txt" 2>/dev/null || true
    [[ -f "$golden" ]] && cp -f "$golden" "$REPORT_DIR/${flow}-flow.golden.txt"
    echo "    $status (recorded; see report)"
  else
    echo "    PASS"
  fi
done

echo
echo "==> summary"
for line in "${SUMMARY[@]}"; do
  echo "    $line"
done

if [[ "$UPDATE" == "1" ]]; then
  exit "$overall"
fi

if [[ "$overall" -ne 0 ]]; then
  echo "==> FAIL: $(wc -l < "$REPORT_DIR/changed.txt" | tr -d ' ') flow(s) changed or new (re-run with --update to accept)" >&2
else
  echo "==> PASS: all ${#FLOWS[@]} flow(s) match their golden"
fi
exit "$overall"
