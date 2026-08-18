# End-to-end recording of `nitro task create` against the fixture workspace.
#
# Exercises task creation with no dependencies/parent, the simplest write
# path. The minted ID is seeded from the task title and wall-clock time (see
# TaskStore.CreateTaskIdAsync), so it is non-deterministic across recordings;
# run.sh's `create` SCRUBS entry normalizes it before diffing. Needs no auth:
# task commands do not call the Nitro backend.
# Produces create-flow.gif + create-flow.txt.

Output create-flow.gif
Output create-flow.txt

Set Shell bash
Set FontSize 18
Set Width 1100
Set Height 850
Set Padding 14
Set TypingSpeed 50ms
Set CursorBlink false

# --- setup (hidden) ---
Hide
Type `export PS1="$ "` Enter
Type `export PATH=/src/src/Nitro/CommandLine/test/e2e/bin:$PATH` Enter
Type `export HOME=/tmp/home` Enter
Type `export NITRO_TASK_ACTOR=e2e-agent` Enter
Type `mkdir -p "$HOME"` Enter
Type `rm -rf /tmp/work && mkdir -p /tmp/work && cp -r /vhs/fixture/acme /tmp/work/acme && cd /tmp/work/acme` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro task create "Write onboarding docs"` Enter

Wait+Screen@30s /Created task '/
Sleep 1500ms
Screenshot create-flow-final.png
Sleep 500ms
