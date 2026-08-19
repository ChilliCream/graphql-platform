# End-to-end recording of `nitro agent tasks close` then `nitro agent tasks reopen` against
# the fixture workspace.
#
# Exercises the close/reopen write paths on a fixture task that starts open
# (fixtures/seed.sql), round-tripping its status back to open. Neither command
# prints a timestamp, so the output stays deterministic without a SCRUBS
# entry. Needs no auth: task commands do not call the Nitro backend.
# Produces close-reopen-flow.gif + close-reopen-flow.txt.

Output close-reopen-flow.gif
Output close-reopen-flow.txt

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
Type `nitro agent tasks close acme-a1b --reason "Won't fix, unreproducible"` Enter

Wait+Screen@30s /Closed task 'acme-a1b'/
Sleep 500ms

Type `nitro agent tasks reopen acme-a1b` Enter

Wait+Screen@30s /Reopened task 'acme-a1b'/
Sleep 1500ms
Screenshot close-reopen-flow-final.png
Sleep 500ms
