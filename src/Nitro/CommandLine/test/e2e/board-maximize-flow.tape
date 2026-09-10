# End-to-end recording of the Tasks tab board's maximize toggle ('z'),
# keystroke-driven, against the fixture workspace.
#
# Exercises BoardLayoutKind.Maximized: 'z' collapses the grid to the focused
# column at full width with a "<index>/<count>" header suffix (rendered as
# "<Name> - <index>/<count> (<count>)" by BoardMode.RenderMaximized +
# ColumnPane.Render), Right moves which column is maximized, and a second
# 'z' returns to the five-column grid. Every transition syncs on a
# Wait+Screen sentinel anchored on a literal column header (with its
# fixture task count) or card row from fixtures/seed.sql, never on Sleep.
# Fixed geometry (Width/Height below) is the stability lever for this
# full-screen layout: changing it requires `--update` of every board-flow*
# golden.
#
# Launched via bare `nitro agent`, same as
# board-flow.tape, so the tab strip is on screen throughout; this flow's own
# job is the maximize toggle, so it does not also drive a tab switch.
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
Type `mkdir -p "$HOME"` Enter
Type `rm -rf /tmp/work && mkdir -p /tmp/work && cp -r /vhs/fixture/acme /tmp/work/acme && cd /tmp/work/acme` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro agent` Enter

# Initial frame: five-column grid, focus on "Blocked" (index 0), selection
# on its first row (acme-a1b).
Wait+Screen@30s /> ○ \[B\] P0 acme-a1b Checkout crashes on Safari/

# z: maximize the focused column ("Blocked", 1 of 5, 3 cards).
Type "z"
Wait+Screen@30s /Blocked - 1\/5 \(3\)/

# Right: the maximized column follows focus to "Deferred" (2 of 5, 1 card).
Right
Wait+Screen@30s /Deferred - 2\/5 \(1\)/

# Right: "Ready" (3 of 5, 0 cards: both open tasks in this fixture are
# blocked, see board-flow.tape's header).
Right
Wait+Screen@30s /Ready - 3\/5 \(0\)/

# Right: "In Progress" (4 of 5, 1 card).
Right
Wait+Screen@30s /In Progress - 4\/5 \(1\)/

# Right: "Closed" (5 of 5, 1 card).
Right
Wait+Screen@30s /Closed - 5\/5 \(1\)/

# z: restore the five-column grid, still focused on "Closed". Final state
# for the golden: the neighboring "In Progress (1)" column header is visible
# again, which only happens outside Maximized layout (Maximized shows one
# column).
Type "z"
Wait+Screen@30s /In Progress \(1\)/

Sleep 1500ms
Screenshot board-maximize-flow-final.png
Sleep 500ms
