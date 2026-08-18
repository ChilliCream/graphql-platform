# BLOCKED (see perles-net-4gb.7 NEEDS-PASCAL comment): `nitro task board`
# never paints a frame under the VHS/ttyd pty used by this harness. It enters
# the alternate screen (confirmed via strace: EnterAltScreen + CursorHome +
# CursorHide ANSI writes happen) and then writes nothing else to stdout,
# indefinitely (verified for 15s+ real time, only Ctrl+C returns to the
# shell). This reproduces both under the pinned VHS container (real 95x35
# geometry, TERM=xterm-256color) and locally, so it is not a marker/geometry
# mistake in this tape. Prepared here so recording can resume with `--update`
# once the underlying Tui/Runtime issue is fixed (out of this bead's lane);
# not wired into run.sh's MARKERS/ALL_FLOWS until then.
#
# End-to-end recording of `nitro task board`, keystroke-driven, against the
# fixture workspace.
#
# The board TUI (raw-key event loop, full-screen alternate-screen rendering)
# cannot be exercised by the in-process test tier; this is the VHS tier's job.
# Drives column focus (Left/Right) and row selection (Up/Down) across the
# fixture's five columns (see fixtures/seed.sql), syncing every transition on
# a Wait+Screen sentinel anchored on the selection prefix ("> ") next to a
# literal card title, never on Sleep. Fixed geometry (Width/Height below) is
# the stability lever for this full-screen layout: changing it requires
# `--update` of every board-flow* golden.
# Produces board-flow.gif + board-flow.txt.

Output board-flow.gif
Output board-flow.txt

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
Type `nitro task board` Enter

# Initial frame: focus starts on the "Blocked" column (index 0), selection on
# its first row (acme-a1b, sorted by priority ahead of acme-epic1.2).
Wait+Screen@30s /> ○ \[B\] P0 acme-a1b Checkout crashes on Safari/

# Down: move selection onto the column's second row (acme-epic1.2).
Down
Wait+Screen@30s /> ○ \[T\] P2 acme-epic1\.2 Wire up billing API/

# Up: back onto the first row (acme-a1b).
Up
Wait+Screen@30s /> ○ \[B\] P0 acme-a1b Checkout crashes on Safari/

# Right: focus "Deferred" (index 1), whose only card is acme-e5f.
Right
Wait+Screen@30s /> ⏸ \[C\] P4 acme-e5f Rotate staging API keys/

# Right: focus "Ready" (index 2), whose only card is acme-epic1.
Right
Wait+Screen@30s /> ○ \[E\] P1 acme-epic1 Launch v2 pricing page/

# Right: focus "In Progress" (index 3), whose only card is acme-epic1.1.
Right
Wait+Screen@30s /> ● \[T\] P2 acme-epic1\.1 Draft new pricing copy/

# Left: back to "Ready" (index 2). Final state for the golden.
Left
Wait+Screen@30s /> ○ \[E\] P1 acme-epic1 Launch v2 pricing page/

Sleep 1500ms
Screenshot board-flow-final.png
Sleep 500ms
