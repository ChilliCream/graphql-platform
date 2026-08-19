# End-to-end recording of `nitro agent tasks init` in a fresh directory.
#
# Exercises the write path that creates the on-disk `.nitro/tasks/` schema and
# derives the task ID prefix from the current directory name, ahead of the
# fixture-backed flows below (which reuse a pre-seeded workspace instead of
# running `init` live). Needs no auth: task commands do not call the Nitro
# backend.
# Produces init-flow.gif + init-flow.txt.

Output init-flow.gif
Output init-flow.txt

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
Type `rm -rf /tmp/work && mkdir -p /tmp/work/acme && cd /tmp/work/acme` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro agent tasks init` Enter

Wait+Screen@30s /Initialized task workspace/
Sleep 1500ms
Screenshot init-flow-final.png
Sleep 500ms
