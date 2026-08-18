# End-to-end terminal recordings (VHS)

This directory drives the **real `nitro` binary through a real PTY**, one
[VHS](https://github.com/charmbracelet/vhs) tape per command flow, and produces
two artifacts per flow from a single recording:

| Artifact | Purpose |
| --- | --- |
| `<flow>-flow.gif` | Animated demo |
| `<flow>-flow.golden.txt` | Final-frame text snapshot, diffed in CI as an integration test |

The same recording is both the demo *and* the assertion: one tape, one run.

## Why a recording

Nothing else in this repository exercises `Program.cs`, real DI wiring, real
`System.CommandLine` parsing, or real terminal rendering of the published
binary. This tier does, through a real PTY. It is the slow, high-confidence
layer: a handful of representative flows, not exhaustive.

## The flows

| Flow | Drives | Validates |
| --- | --- | --- |
| `help` | `nitro task list --help` | the pipeline itself: publish, record, extract, diff, end to end |

This is a trivial smoke flow, shipped so the pipeline is testable before the
fixture-backed `nitro task` flows land (separate bead). `task` is a hidden
top-level command (`Hidden = true` on `TaskCommand`): `nitro task --help`
renders nothing at all (System.CommandLine exits 0 with empty output for a
hidden command's own `--help`), so this flow asks for help on one of its
visible subcommands instead. Task commands need no auth, they never call the
Nitro backend.

Real task flows (`create`, `list`, `show`, ...) against a deterministic fixture
workspace land in a separate bead; keep `MARKERS`/`ALL_FLOWS` in
[`run.sh`](run.sh) in sync with the tape set as they are added.

## How it works

```
<flow>-flow.tape --> VHS container (ttyd + ffmpeg) --> <flow>-flow.gif
                                                    \-> <flow>-flow.txt --> extract-frame.sh --> diff vs golden
```

1. **`<flow>-flow.tape`** is a VHS script. A hidden setup block puts the
   published binary on `PATH`, pins `HOME=/tmp/home` (hermetic), and works in a
   throwaway `/tmp/work`. Recorded steps gate on `Wait+Screen /.../` sentinels,
   so the recording syncs on **state** rather than wall-clock timing.
2. **`run.sh`** publishes a self-contained `linux-x64` binary once, then for
   each flow mounts the repo read-only into the pinned VHS container and
   records.
3. **`extract-frame.sh`** reduces VHS's multi-frame `.txt` capture to the final
   completed frame, keyed on a per-flow marker (e.g. `List tasks.`).
4. An optional per-flow `SCRUBS` entry in `run.sh` (a `sed -E` expression) can
   normalize non-deterministic content, such as wall-clock-seeded task IDs
   (`acme-[a-z0-9]+`) or dates, before the frame is compared with or written as
   the golden. Empty by default; a future task flow that creates state live
   would add its own entry.
5. Each frame is diffed against `<flow>-flow.golden.txt`.

## Running it

```bash
./run.sh                    # record + verify EVERY flow (what CI does)
./run.sh help                # record + verify only the named flow(s)
./run.sh --update            # accept new output: refresh all goldens + GIFs
./run.sh --update help       # refresh a single flow
REBUILD=1 ./run.sh           # force re-publish of the binary first
NITRO_E2E_AOT=1 ./run.sh     # publish with PublishAot=true instead (slow, matches the shipped binary)
```

`run.sh` exits non-zero if any flow's frame differs from its golden (**FAIL**)
or has no golden yet (**NEW**), and collects the changed/new GIFs, frames, and
diffs under `out/report/` for CI. Exit codes: `0` all PASS, `1` any FAIL/NEW
(or, under `--update`, any recording failure), `2` usage error, `3` docker
missing.

Requirements: `docker` + the .NET SDK. Nothing else, `ttyd`, `ffmpeg`, and the
fonts are baked into the pinned container.

## What makes it deterministic

Two independent runs produce a **byte-identical** final frame. The levers:

- **Assert on text, never on the GIF.** GIF bytes go through ffmpeg/gifski and
  are not stable across versions/platforms. The character grid (`.txt`) is.
- **Final frame only.** Intermediate frames vary with timing; the end state
  does not.
- **Pinned VHS image** (by digest), a new VHS release can't silently reflow
  output.
- **Fixed geometry / theme / `CursorBlink false`** in every tape.
- **Hermetic inputs**: a fixed `/tmp/work` cwd and a pinned `$HOME` where it
  appears in output, so every path is constant.
- **Per-flow `SCRUBS`** normalize anything still non-deterministic (task IDs
  seeded from wall-clock time, dates) before the diff.

If `nitro` legitimately changes its output, the diff fails, that's the test
working. Re-run with `--update <flow>` and commit the new golden + GIF.

## Files

| File | |
| --- | --- |
| `<flow>-flow.tape` | The VHS script for a flow (source of truth) |
| `<flow>-flow.golden.txt` | Committed final-frame snapshot |
| `<flow>-flow.gif` | Committed demo (regenerate with `run.sh --update <flow>`) |
| `run.sh` | Publish, record each flow, extract, verify/update + build report |
| `extract-frame.sh` | VHS `.txt` -> final frame (per-flow marker) |
| `bin/`, `out/` | gitignored: published binary, raw recordings, report |

Non-goals of this bead: fixture seeding, the real task flows and their tapes,
and CI wiring all land in separate beads. Linux `linux-x64` only, no
Windows/macOS support, matching the upstream skillz e2e tier this pipeline was
ported from.
