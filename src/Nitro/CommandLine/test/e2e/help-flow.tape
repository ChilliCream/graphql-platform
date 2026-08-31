# End-to-end recording of `nitro agent tasks list --help`.
#
# Trivial pipeline smoke flow: proves publish -> record -> extract -> diff works
# end to end (real Program.cs, real DI wiring, real System.CommandLine parsing,
# real terminal rendering of the published binary) independently of the
# fixture-backed agent tasks flows. This flow asks for help on `list`, a leaf
# subcommand, since its help output is the more informative pipeline check
# than the `agent`/`tasks` group commands' own help. Needs no auth: task
# commands do not call the Nitro backend.
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
Type `nitro agent tasks list --help` Enter

Wait+Screen@30s /List tasks\./
Sleep 1500ms
Screenshot help-flow-final.png
Sleep 500ms
