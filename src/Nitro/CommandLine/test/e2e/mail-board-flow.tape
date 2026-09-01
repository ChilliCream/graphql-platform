# End-to-end recording of the read-only Mail tab, keystroke-driven, against
# the unified fixture workspace. Bare `nitro agent` has no actor identity, so
# the board observes the Workspace mailbox and refuses identity-dependent
# writes or Inbox/Sent jumps.
#
# The flow covers the Workspace thread list, the read-only footer, row
# selection, fold expansion, flat list mode, the detail thread toggle, and
# the per-agent Workspace filter. Fixed fixture timestamps keep every rendered
# age deterministic, so this flow needs no SCRUBS entry.
# Produces mail-board-flow.gif + mail-board-flow.txt.

Output mail-board-flow.gif
Output mail-board-flow.txt

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

# Initial frame: the Tasks tab's board.
Wait+Screen@30s /> ○ \[B\] P0 acme-a1b Checkout crashes on Safari/

# Move to Mail. Actor-less boards always open the read-only Workspace mailbox.
Type "]"
Wait+Screen@30s /Workspace \(5\)/
Wait+Screen@30s /From\s+To\s+Age/
Wait+Screen@30s /f filter/

# Select the "Retro notes" thread.
Down
Down
Wait+Screen@30s /Thread: Retro notes/

# Expand the selected thread in the list pane.
Type "z"
Type "o"
Wait+Screen@30s />\s+▾/
Wait+Screen@30s /└\s+alice/
Wait+Screen@30s /└\s+e2e-agent\s+alice/

# Switch from five thread rollups to the six-message flat stream.
Type "V"
Wait+Screen@30s /Workspace \(6\)/

# Toggle the selected message's detail pane to its full thread.
Type "t"
Wait+Screen@30s /Notes are attached, take a look\./

# Filter the Workspace mailbox to bob. The picker is ordered as All agents,
# alice, bob, e2e-agent, so bob is two rows below the default selection.
Type "p"
Wait+Screen@30s /Filter by agent/
Down
Down
Enter
Wait+Screen@30s /Workspace: bob \(3\)/

Sleep 1500ms
Screenshot mail-board-flow-final.png
Sleep 500ms
