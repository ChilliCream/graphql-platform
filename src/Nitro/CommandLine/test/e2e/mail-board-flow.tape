# End-to-end recording of the read-only Mail tab, keystroke-driven, against
# the unified fixture workspace. Bare `nitro agent` has no actor identity, so
# the board observes the Workspace mailbox and refuses identity-dependent
# writes or Inbox/Sent jumps.
#
# The flow covers the Mail tab's full-width thread table (one row per thread:
# subject, from, to, message count, and last-activity columns), the per-agent
# Workspace filter, and the Enter thread popover (mirroring board-flow.tape's/
# agents-flow.tape's structure). Fixed fixture timestamps keep every rendered
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

# Move to Mail. Actor-less boards always open the read-only Workspace mailbox,
# rendered as one full-width table of every thread (MailTable/MailMode).
Type "]"
Wait+Screen@30s /Mail \(5\)/
Wait+Screen@30s /SUBJECT\s+FROM\s+TO\s+MSGS\s+ACTIVITY/

# Filter the Workspace mailbox to bob. The picker is ordered as All agents,
# alice, bob, e2e-agent, so bob is two rows below the default selection.
Type "p"
Wait+Screen@30s /Filter by agent/
Down
Down
Enter
Wait+Screen@30s /Mail: bob \(3\)/

# Enter opens the first (and, sorted by last activity, most recent) row's
# thread in a read-only popover: the subject as its title, a Participants
# header field, and the thread's messages oldest first.
Enter
Wait+Screen@30s /Billing review/
Wait+Screen@30s /Participants:/
Wait+Screen@30s /Needs review before merge\./

# Esc closes the popover, back on the filtered table.
Escape
Wait+Screen@30s /Mail: bob \(3\)/

Sleep 1500ms
Screenshot mail-board-flow-final.png
Sleep 500ms
