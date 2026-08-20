# End-to-end recording of `nitro agent tasks list` against the fixture workspace.
#
# Exercises the default (open, non-tombstone) listing over a real
# `.nitro/tasks/tasks.db`, ordered by priority/created_at/id, against the
# deterministic dataset in fixtures/seed.sql. Needs no auth: task commands do
# not call the Nitro backend.
# Produces list-flow.gif + list-flow.txt.

Output list-flow.gif
Output list-flow.txt

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
Type `nitro agent tasks list` Enter

Wait+Screen@30s /acme-e5f/
Sleep 1500ms
Screenshot list-flow-final.png
Sleep 500ms
