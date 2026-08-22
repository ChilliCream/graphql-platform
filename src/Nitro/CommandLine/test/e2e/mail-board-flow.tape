# End-to-end recording of the Mail tab's board, keystroke-driven, against
# the unified fixture workspace (task data from fixtures/seed.sql, mail data
# from fixtures/mail-seed.sql, both in the one out/fixture/acme agents.db),
# mirroring board-flow.tape's structure for the task board.
#
# Launched via bare `nitro agent` (not `agent mail board`), landing on the
# Tasks tab first, then TabSwitchKeys (']') moves to Mail -- the same
# round trip board-flow.tape demonstrates in the other direction, and the
# tab strip is on screen throughout.
#
# The mail board TUI (raw-key event loop, full-screen alternate-screen
# rendering) cannot be exercised by the in-process test tier; this is the
# VHS tier's job. Drives row selection (Down) to the fixture's oldest inbox
# message, opens its detail pane (Enter, also marking it read), then toggles
# the thread view ('t') to reveal its reply -- unread styling, the detail
# pane, and the thread toggle. From there it jumps mailboxes with the direct
# Shift+letter chords (MailKeyMap): Shift+S to Sent, showing both of
# e2e-agent's own sent messages with their "To " peer values; Shift+W to
# Workspace, showing every agent's mail including one message between two
# other agents that never involves e2e-agent at all, plus Workspace's
# read-only mode indicator (its list header names the mailbox); Shift+I back
# to Inbox, the flow's final state. Together these cover the Sent mailbox,
# the Peer column's "To " prefix, the relationship glyph, and the Workspace
# observer mode -- the mail e2e tape bead's scope (not exhaustive
# per-command coverage, that is the unit tier's job).
#
# fixtures/mail-seed.sql gives e2e-agent three inbox messages (it is a `to`
# recipient of each): m-fix002 (bob, already read, newest by created_at),
# m-fix001 (alice, unread), m-fix003 (alice, unread, the root of a thread
# whose reply, m-fix004, is e2e-agent's own and so never appears in the
# inbox list itself, only via the thread toggle). e2e-agent also sent
# m-fix005, an unreplied thread root reachable only through Sent; alice sent
# m-fix006 to bob, reachable only through Workspace. Every seeded
# `created_at` is far enough in the past that MailAges' age column always
# renders as a fixed "yyyy-MM-dd" string, independent of the wall-clock date
# a recording actually runs on, so this flow needs no SCRUBS entry (unlike
# mail-send-flow, which creates state live against the real clock).
#
# Fixed geometry (Width/Height below) is the stability lever for this
# full-screen layout, same as board-flow.tape: changing it requires
# `--update` of this flow's golden.
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
Type `export NITRO_MAIL_ACTOR=e2e-agent` Enter
Type `mkdir -p "$HOME"` Enter
Type `rm -rf /tmp/work && mkdir -p /tmp/work && cp -r /vhs/fixture/acme /tmp/work/acme && cd /tmp/work/acme` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro agent` Enter

# Initial frame: the Tasks tab's board (same sentinel as board-flow.tape's
# first Wait+Screen).
Wait+Screen@30s /> ○ \[B\] P0 acme-a1b Checkout crashes on Safari/

# ']': TabSwitchKeys moves to the Mail tab. List pane titled "Inbox (3)",
# selection on the first row (m-fix002, bob, newest, already read).
Type "]"
Wait+Screen@30s /Inbox \(3\)/

# Down, Down: onto the third row (m-fix003, alice, unread, its sole
# recipient -- the thread root). The unread marker ('*') and the '+' direct
# relationship glyph sit directly before the peer name regardless of the
# row's exact selection/subject rendering, so the wait anchors on
# marker+glyph+peer rather than the (width-truncatable) subject.
Down
Down
Wait+Screen@30s /> \* \+ alice /

# Enter: focus the detail pane for the selected message (also marks it read
# for e2e-agent).
Enter
Wait+Screen@30s /From: alice/

# 't': toggle the detail pane from the single message to its whole thread,
# revealing e2e-agent's own reply (m-fix004), which is never listed in the
# inbox itself.
Type "t"
Wait+Screen@30s /Thanks, looks good to me\./

# Shift+S: direct jump to the Sent mailbox. List pane titled "Sent (2)",
# selection on the first row (m-fix004, the newest of e2e-agent's own sent
# messages -- otherwise reachable only via the thread toggle above). Its
# Peer column reads "To alice" (the FromActorGlyph 'F' plus the "To "-
# prefixed recipient); the second row, m-fix005 (an unreplied thread root
# with no other way into the TUI), reads "To bob".
Type "S"
Wait+Screen@30s /Sent \(2\)/
Wait+Screen@30s /F To alice/
Wait+Screen@30s /F To bob/

# Shift+W: direct jump to the Workspace mailbox. List pane titled
# "Workspace (6)", the whole workspace's mail across every agent, including
# m-fix006 (alice to bob), which never involves e2e-agent -- its Peer column
# reads the bare sender name "alice" with a blank relationship glyph, since
# e2e-agent is neither its sender nor a recipient. The list pane's header
# naming the mailbox is Workspace's redundant read-only mode indicator (see
# MailMode's class doc).
Type "W"
Wait+Screen@30s /Workspace \(6\)/
Wait+Screen@30s /alice Deploy win/

# Shift+I: direct jump back to Inbox. Final state for the golden.
Type "I"
Wait+Screen@30s /Inbox \(3\)/

Sleep 1500ms
Screenshot mail-board-flow-final.png
Sleep 500ms
