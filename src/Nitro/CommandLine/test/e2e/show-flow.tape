# End-to-end recording of `nitro agent tasks show` against the fixture workspace.
#
# Exercises the detail render: header, dependencies, and blocks sections, over
# a task with both an outgoing parent-child dependency and an incoming block
# (fixtures/seed.sql). Created/Updated render deterministically because the
# fixture's timestamps are hardcoded. Needs no auth: task commands do not
# call the Nitro backend.
# Produces show-flow.gif + show-flow.txt.

Output show-flow.gif
Output show-flow.txt

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
Type `nitro agent tasks show acme-epic1.1` Enter

Wait+Screen@30s /Draft new pricing copy/
Sleep 1500ms
Screenshot show-flow-final.png
Sleep 500ms
