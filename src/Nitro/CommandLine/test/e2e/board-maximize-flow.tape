# BLOCKED (see perles-net-4gb.7 NEEDS-PASCAL comment): `nitro task board`
# never paints a frame under the VHS/ttyd pty used by this harness (confirmed
# via strace and 15s+ real-time waits; see board-flow.tape's header for the
# full reproduction). Prepared here so recording can resume with `--update`
# once the underlying Tui/Runtime issue is fixed (out of this bead's lane);
# not wired into run.sh's MARKERS/ALL_FLOWS until then.
#
# End-to-end recording of `nitro task board`'s maximize toggle ('z'),
# keystroke-driven, against the fixture workspace.
#
# Exercises BoardLayoutKind.Maximized: 'z' collapses the grid to the focused
# column at full width with a "<index>/<count>" header suffix, Right moves
# which column is maximized, and a second 'z' returns to the five-column
# grid. Every transition syncs on a Wait+Screen sentinel anchored on a
# literal column header (with its fixture task count) from
# fixtures/seed.sql, never on Sleep. Fixed geometry (Width/Height below) is
# the stability lever for this full-screen layout: changing it requires
# `--update` of every board-flow* golden.
# Produces board-maximize-flow.gif + board-maximize-flow.txt.

Output board-maximize-flow.gif
Output board-maximize-flow.txt

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

# Initial frame: five-column grid, focus on "Blocked" (index 0).
Wait+Screen@30s /Blocked \(2\)/

# z: maximize the focused column ("Blocked", 1 of 5).
Type "z"
Wait+Screen@30s /Blocked \(2\) - 1\/5/

# Right: the maximized column follows focus to "Deferred" (2 of 5).
Right
Wait+Screen@30s /Deferred \(1\) - 2\/5/

# Right: "Ready" (3 of 5).
Right
Wait+Screen@30s /Ready \(1\) - 3\/5/

# z: restore the five-column grid, still focused on "Ready". Final state for
# the golden: the neighboring "In Progress" column header is visible again,
# which only happens outside Maximized layout (Maximized shows one column).
Type "z"
Wait+Screen@30s /In Progress \(1\)/

Sleep 1500ms
Screenshot board-maximize-flow-final.png
Sleep 500ms
