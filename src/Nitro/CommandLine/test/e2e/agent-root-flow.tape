# End-to-end recording of bare `nitro agent`, keystroke-driven, against the
# fixture workspace: the tape that proves the unified tabbed TUI mount at the
# bare group command end to end (bd-agent-unify-814.4), not just via the
# `agent tasks board`/`agent mail board` single-tab entry points the other
# TUI flows drive.
#
# Bare `nitro agent` against an existing agent workspace launches the
# tabbed shell on the Tasks tab (AgentCommand.ExecuteAsync ->
# AgentTuiLauncher.RunAsync); board-flow.tape exercises switching tabs with
# '['/']', so this flow's own job is narrower: prove the mount itself, then
# quit cleanly back to the shell ('q' -> TuiMessage.QuitRequested renders the
# confirm dialog, 'y' -> TuiMessage.ConfirmQuit stops TuiApplication's event
# loop and returns control to bash). The chained echo after quitting
# (mirroring mail-send-flow.tape's chained-command Sleep 500ms pattern)
# proves the shell, not the TUI's alternate screen, is back in control for
# the golden's final frame.
# Produces agent-root-flow.gif + agent-root-flow.txt.

Output agent-root-flow.gif
Output agent-root-flow.txt

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
Type `nitro agent` Enter

# Initial frame: same sentinel as board-flow.tape's first Wait+Screen (the
# Tasks tab's board, focus on "Blocked", selection on acme-a1b), now with
# the tab strip rendered above it.
Wait+Screen@30s /> ○ \[B\] P0 acme-a1b Checkout crashes on Safari/

# 'q' requests quit; the shell renders the "Quit? (y/n)" confirmation dialog.
Type "q"
Wait+Screen@30s /Quit\? \(y\/n\)/
Sleep 500ms

# 'y' confirms, the TUI exits, and the shell prompt returns.
Type "y"
Sleep 500ms
Type `echo "agent-root-flow: back at the shell"` Enter

Wait+Screen@30s /agent-root-flow: back at the shell/
Sleep 1500ms
Screenshot agent-root-flow-final.png
Sleep 500ms
