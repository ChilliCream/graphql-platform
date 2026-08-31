# End-to-end recording of bare `nitro agent`'s Tasks tab (the board), against
# the fixture workspace.
#
# The board TUI (raw-key event loop, full-screen alternate-screen rendering)
# cannot be exercised by the in-process test tier; this is the VHS tier's job.
# Drives column focus (Left/Right) and row selection (Up/Down) across all
# five of the fixture's columns (see fixtures/seed.sql and BoardView.Default),
# syncing every transition on a Wait+Screen sentinel anchored on the
# selection prefix ("> ") next to a literal card title, or (for the empty
# "Ready" column) the focused panel's own box-drawing top border, never on
# Sleep. Fixed geometry (Width/Height below) is the stability lever for this
# full-screen layout: changing it requires `--update` of every board-flow*
# golden.
#
# Column membership below is computed by BoardDataLoader/TaskStore
# ComputeBlockedAsync against fixtures/seed.sql, not just each task's own
# `status`: acme-epic1 is an open epic with a non-terminal child
# (acme-epic1.2), so it lands in "Blocked" (not "Ready"); acme-epic1.2 is
# open but blocked by the still-in_progress acme-epic1.1, so it is also in
# "Blocked"; acme-a1b is blocked by acme-epic1.2. That leaves the "Ready"
# column empty for this fixture.
#
# Launched via bare `nitro agent` so the tab strip
# is on screen throughout, and closes by demonstrating the epic's visible
# feature: TabSwitchKeys ('[' / ']') moves to the Mail tab and back, landing
# on the same "In Progress" selection the board-only recording used to end
# on, proving each tab's mode stack (here, board selection) survives the
# round trip.
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
Type `mkdir -p "$HOME"` Enter
Type `rm -rf /tmp/work && mkdir -p /tmp/work && cp -r /vhs/fixture/acme /tmp/work/acme && cd /tmp/work/acme` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro agent` Enter

# Initial frame: focus starts on "Blocked" (index 0, 3 cards), selection on
# its first row (acme-a1b, P0, sorted ahead of acme-epic1 P1 and
# acme-epic1.2 P2).
Wait+Screen@30s /> ○ \[B\] P0 acme-a1b Checkout crashes on Safari/

# Down: move selection onto the column's second row (acme-epic1, an epic
# blocked by its own open children).
Down
Wait+Screen@30s /> ○ \[E\] P1 acme-epic1 Launch v2 pricing page/

# Down: onto the third row (acme-epic1.2).
Down
Wait+Screen@30s /> ○ \[T\] P2 acme-epic1\.2 Wire up billing API/

# Up: back onto the second row (acme-epic1).
Up
Wait+Screen@30s /> ○ \[E\] P1 acme-epic1 Launch v2 pricing page/

# Right: focus "Deferred" (index 1), whose only card is acme-e5f.
Right
Wait+Screen@30s /> ⏸ \[C\] P4 acme-e5f Rotate staging API keys/

# Right: focus "Ready" (index 2). Empty for this fixture (both open tasks
# are blocked), so sync on the focused panel's own top border instead of a
# card row.
Right
Wait+Screen@30s /╭─Ready \(0\)─/

# Right: focus "In Progress" (index 3), whose only card is acme-epic1.1.
Right
Wait+Screen@30s /> ● \[T\] P2 acme-epic1\.1 Draft new pricing copy/

# Right: focus "Closed" (index 4), whose only card is acme-c3d.
Right
Wait+Screen@30s /> ✓ \[F\] P3 acme-c3d Add dark mode toggle/

# Left: back to "In Progress".
Left
Wait+Screen@30s /> ● \[T\] P2 acme-epic1\.1 Draft new pricing copy/

# ']': TabSwitchKeys moves to the next tab (Mail), whose data comes from
# the same fixture's mail-seed.sql (see mail-board-flow.tape). MailState
# defaults to the Workspace mailbox, so the list pane
# opens titled "Workspace (5)", not Inbox.
Type "]"
Wait+Screen@30s /Workspace \(5\)/

# '[': back to the Tasks tab. The board's mode stack was never touched while
# Mail was active, so the selection is exactly where it was left ("In
# Progress", acme-epic1.1). Final state for the golden.
Type "["
Wait+Screen@30s /> ● \[T\] P2 acme-epic1\.1 Draft new pricing copy/

Sleep 1500ms
Screenshot board-flow-final.png
Sleep 500ms
