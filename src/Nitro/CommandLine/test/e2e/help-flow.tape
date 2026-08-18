# End-to-end recording of `nitro task list --help`.
#
# Trivial pipeline smoke flow: proves publish -> record -> extract -> diff works
# end to end (real Program.cs, real DI wiring, real System.CommandLine parsing,
# real terminal rendering of the published binary) before fixture-backed task
# flows land (separate bead). `task` itself is a hidden top-level command
# (System.CommandLine renders no help at all for `nitro task --help`, exit 0,
# empty output), so this flow asks for help on one of its visible subcommands
# instead. Needs no auth: task commands do not call the Nitro backend.
# Produces help-flow.gif + help-flow.txt.

Output help-flow.gif
Output help-flow.txt

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
Type `cd /tmp && rm -rf work && mkdir work && cd work` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro task list --help` Enter

Wait+Screen@30s /List tasks\./
Sleep 1500ms
Screenshot help-flow-final.png
Sleep 500ms
