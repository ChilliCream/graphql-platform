# End-to-end recording of the Memory tab: the unified curated/journal table,
# its kind filter, and the Enter entry popover, against the deterministic
# fixture workspace (fixtures/seed.sql + mail-seed.sql + agents-seed.sql +
# memory-seed.sql), mirroring board-flow.tape's/mail-board-flow.tape's/
# agents-flow.tape's structure.
#
# Launched via bare `nitro agent`, landing on the Tasks tab first (the same
# first frame every other bare-launch tape asserts), then Shift+E
# (TabSwitchKeys.ResolveMnemonic, the tab strip's M[e]mory mnemonic) jumps
# straight to Memory.
#
# The Memory tab shows every curated memory and journal entry in the
# workspace, not just this flow's own: fixtures/agents-seed.sql (applied to
# the same shared fixture for agents-flow.tape's popover walkthrough) already
# seeds three journal entries for `nora`, so fixtures/memory-seed.sql's own
# two curated memories and one journal entry bring the default "All" view to
# six rows total ("Memory (6)"), not three. 'f' (MemoryState.
# CycleFilterAsync) cycles All -> Curated -> Journal -> All; pressing it
# twice lands on Journal, whose four rows are nora's three plus this flow's
# own ("Journal (4)"), sorted most recent first: nora's two newer entries,
# then this flow's own entry and nora's oldest tie-broken by id ("e2emem..."
# sorts before "e2enramemj..."), so this flow's own entry is the table's
# third Journal row. Two more `j` presses select it; Enter then opens it in
# a read-only popover titled with its own id (MemoryEntryPopoverView.
# BuildHeader), showing "Kind: journal" and its wrapped body; Escape closes
# it, back on the filtered table.
#
# Every fixture timestamp is fixed far enough in the past (2026-01-01) that
# every rendered age lands in AgentAges.Format's/MailAges.Format's "week or
# older" branch, independent of the wall-clock date a recording actually
# runs on, so this flow needs no SCRUBS entry.
#
# Produces memory-board-flow.gif + memory-board-flow.txt.

Output memory-board-flow.gif
Output memory-board-flow.txt

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

# Initial frame: the Tasks tab's board (same sentinel every other bare-launch
# tape asserts first).
Wait+Screen@30s /> ○ +B +P0 +acme-a1b +Checkout crashes on Safari/

# Shift+E: TabSwitchKeys.ResolveMnemonic jumps straight to the Memory tab.
Type "E"
Wait+Screen@30s /Memory \(6\)/
Wait+Screen@30s /KIND\s+TYPE\s+TAGS\s+AGE/

# 'f' cycles the kind filter: All -> Curated -> Journal. Pressing it twice
# lands on Journal (nora's three seeded entries plus this flow's own).
Type "f"
Type "f"
Wait+Screen@30s /Journal \(4\)/

# This flow's own journal entry is the third Journal row (see header
# comment); two `j` presses move the selection onto it. The table no longer
# shows its body, so the popover below is this step's own assertion that
# the right row was reached.
Type "j"
Type "j"

# Enter opens the selected row's read-only popover: its own id as the title,
# its Kind field, and its wrapped body.
Enter
Wait+Screen@30s /e2emem00000000000000000003/
Wait+Screen@30s /Kind: journal/
Wait+Screen@30s /Follow up on the flaky checkout test\./

# Esc closes the popover, back on the filtered table.
Escape
Wait+Screen@30s /Journal \(4\)/

Sleep 1500ms
Screenshot memory-board-flow-final.png
Sleep 500ms
