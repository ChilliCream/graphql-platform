# End-to-end recording of a failing `nitro agent tasks` command against the fixture
# workspace.
#
# Exercises the non-zero-exit UX for a task ID that does not exist
# (TaskStore.GetRequiredTaskAsync -> ExitException, caught by
# SetActionWithExceptionHandling, exit code 1). The exit code is echoed right
# after the command, mirroring the skillz "[skills exit: 1]" trick, since VHS
# only asserts on rendered screen text, never on a process exit code. Needs no
# auth: task commands do not call the Nitro backend.
# Produces error-flow.gif + error-flow.txt.

Output error-flow.gif
Output error-flow.txt

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
Type `mkdir -p "$HOME"` Enter
Type `rm -rf /tmp/work && mkdir -p /tmp/work && cp -r /vhs/fixture/acme /tmp/work/acme && cd /tmp/work/acme` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro agent tasks show acme-nope; echo "[nitro exit: $?]"` Enter

Wait+Screen@30s /\[nitro exit: 1\]/
Sleep 1500ms
Screenshot error-flow-final.png
Sleep 500ms
