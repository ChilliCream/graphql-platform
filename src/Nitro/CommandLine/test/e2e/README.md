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
| `help` | `nitro agent tasks list --help` | the pipeline itself: publish, record, extract, diff, end to end |
| `init` | `nitro agent init` in a fresh directory | the unified `.nitro/agents` workspace path and prefix output |
| `agent-root` | bare `nitro agent`, then quit | the tabbed TUI mounts at the bare group command and quits cleanly |
| `list` / `show` / `create` / `close-reopen` / `dep-tree` / `error` | `nitro agent tasks <cmd>` over the fixture | the non-interactive task commands |
| `board` | the Tasks tab (bare `nitro agent`) | column/row navigation, and a tab switch to Mail and back |
| `board-maximize` | the Tasks tab's maximize toggle (`z`) | the single-column maximized layout |
| `search` | search mode (`/`) in the Tasks tab (bare `nitro agent`) | live query filtering and opening a result's detail pane |
| `detail` | the Tasks tab's detail pane and dependency tree (`t`, `d`, `u`) | the detail body and tree explorer navigation |
| `mail-send` | `nitro agent init` then `agent register/mail send/inbox/reply/read --thread` | the mail send/inbox/reply/read round trip in a live workspace, one actor registered with a role |
| `mail-error` | `nitro agent mail send` to an invalid recipient name | the agent-name-normalization rejection and non-zero exit rendering |
| `mail-board` | the Mail tab (bare `nitro agent`, `]` to switch) | unread styling, the detail pane, and the thread toggle |

`help` is a trivial smoke flow that proves the pipeline itself,
independently of the fixture-backed flows below it. It asks for help on
`list`, a leaf subcommand, since its help output is the more informative
pipeline check than the `agent`/`tasks` group commands' own help. Agent
commands need no auth, they never call the Nitro backend.

`init` and `agent-root` exercise the two ways a bare command opens something:
`nitro agent init` creates the unified workspace in a fresh directory (no
fixture); `nitro agent` against an *existing* workspace instead mounts the
tabbed TUI on the Tasks tab and, driven with no other arguments, is the tape
that proves that mount end to end.

The task board/detail flows (`board`, `board-maximize`, `detail`) and the
mail board flow (`mail-board`) all launch via bare `nitro agent`, so the tab
strip renders throughout;
`board` and `mail-board` each demonstrate switching tabs with `[`/`]` in one
direction, together covering the round trip.

The `mail-*` flows are a handful of representative mail flows, not
exhaustive per-command coverage (that is the unit tier's job): `mail-send`
runs live against a fresh workspace (init via the unified `nitro agent
init`, register two actors via the root `nitro agent register` command
(one with `--role`), send with `--cc`, inbox as the recipient, read with
`--thread` after a reply), so its ids and dates need the `mail-send`
SCRUBS entry in [`run.sh`](run.sh); `mail-board` runs against the shared
seeded fixture ([`fixtures/mail-seed.sql`](fixtures/mail-seed.sql)) with
timestamps fixed far enough in the past that the board's age column always
renders a fixed date, so it needs no SCRUBS entry. Mail commands need no
auth, they never call the Nitro backend.

Keep `MARKERS`/`ALL_FLOWS` in [`run.sh`](run.sh) in sync with the tape set as
flows are added.

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

Linux `linux-x64` only, no Windows/macOS support, matching the upstream skillz
e2e tier this pipeline was ported from.
