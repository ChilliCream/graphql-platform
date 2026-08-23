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
# VHS tier's job. Since perles-net-nql.2 the list pane is a table: a heading
# row (From/To/Age, plus a message-count column in the threaded default)
# above thread-rollup rows, newest activity first, collapsed by default; the
# board opens on the Workspace mailbox rather than Inbox, and a message (or
# thread containing one) unread AND addressed to the actor renders with the
# unread-to-me marker ('●') wherever it appears, including Workspace. Drives:
# row selection (Down x2) onto the fixture's "Retro notes" thread, whose last
# message is the actor's own reply -- the detail pane already shows the
# whole thread for a selected thread row (no 't' needed, unlike a selected
# flat message row); 'z' 'o' (MailKeyMap's fold prefix) expands that thread
# row in the LIST pane itself, revealing its two messages indented beneath
# it with the distinct thread-membership glyph ('└'), separate from the
# fold toggle ('▸'/'▾') on the parent row. Shift+V (MailKeyMap.ToggleListModeRequested)
# swaps the list pane from threaded rows to the pre-epic flat per-message
# stream (kept, not deleted); Shift+S then jumps to Sent, still in flat mode
# (list mode is an axis independent of mailbox), showing both of e2e-agent's
# own sent messages; Shift+I jumps back to Inbox, the flow's final state.
# Together these cover the epic's five call-outs (Workspace default, the
# heading row, expanding a thread, the unread-to-me highlight, and the
# Shift+V flat-mode toggle -- Shift+T was reassigned to Shift+V during
# nql.2's review since Shift+T collides with the Tasks tab mnemonic and
# lowercase t is already the detail pane's own thread toggle, see
# perles-net-nql.2 comment 56) alongside the Sent/Inbox mailbox jumps the
# mail e2e tape bead (perles-net-wi3.4) established -- not exhaustive
# per-command coverage, that is the unit tier's job.
#
# fixtures/mail-seed.sql gives e2e-agent three inbox messages (it is a `to`
# recipient of each): m-fix002 (bob, already read, newest by created_at,
# also cc'd to alice so its relationship glyph is the broadcast 'T'),
# m-fix001 (alice, unread), m-fix003 (alice, unread, the root of a thread
# whose reply, m-fix004, is e2e-agent's own). As a thread rollup, m-fix003's
# row reports its THREAD's last message (m-fix004): sender e2e-agent (the
# from-me glyph 'F'), recipient alice, while still carrying the unread-to-me
# marker because m-fix003 itself, still unread, is part of the same thread
# (collapsed rows roll up unread with any-message semantics). e2e-agent also
# sent m-fix005, an unreplied thread root reachable only through Sent; alice
# sent m-fix006 to bob, reachable only through Workspace. Every seeded
# `created_at` is far enough in the past that MailAges' age column always
# renders as a fixed "yyyy-MM-dd" string, independent of the wall-clock date
# a recording actually runs on, so this flow needs no SCRUBS entry (unlike
# mail-send-flow, which creates state live against the real clock).
#
# Fixed geometry (Width/Height below) is the stability lever for this
# full-screen layout, same as board-flow.tape: changing it requires
# `--update` of this flow's golden. At this geometry the list pane's fixed
# columns (prefix/From/To/Age/count) already claim the full pane width, so
# Subject/Preview render at zero width and the count column's heading and
# per-row values wrap onto their own line beneath each row -- the table's
# own documented degradation at a narrower-than-200-col pane (see MailTable's
# ComputeColumns), not a bug in this tape.
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

# ']': TabSwitchKeys moves to the Mail tab. MailState defaults to the
# Workspace mailbox (perles-net-nql.2), so the list pane opens titled
# "Workspace (5)" -- five thread rollups across every agent's mail -- with
# the heading row (From/To/Age; the count column's own "#" heading wraps
# beneath it at this width, see this tape's header comment) above them, and
# selection on the newest thread (m-fix002, bob, its cc to alice giving it
# the broadcast 'T' relationship glyph).
Type "]"
Wait+Screen@30s /Workspace \(5\)/
Wait+Screen@30s /From\s+To\s+Age/
Wait+Screen@30s />\s+▸\s+T\s+bob/

# Down, Down: onto the "Retro notes" thread row (m-fix003 + e2e-agent's own
# reply m-fix004). As a rollup its From/glyph report the THREAD's last
# message (e2e-agent, the from-me 'F' glyph) while the unread-to-me marker
# ('●') still lights up the row, because m-fix003 itself is unread to
# e2e-agent (any-message rollup semantics) -- Workspace's own read-only mode
# never suppresses this marker, since it is always the actor's own state.
# The detail pane already renders the whole thread for a selected thread
# row (the epic's default, no 't' needed here unlike a flat message row).
Down
Down
Wait+Screen@30s />\s+▸\s+●\s+F\s+e2e-agent/
Wait+Screen@30s /Thread: Retro notes/

# 'z' 'o': MailKeyMap's fold prefix, then open -- expands the selected
# thread row in the LIST pane itself. The parent row's fold glyph flips
# from collapsed ('▸') to expanded ('▾'); its two messages appear indented
# beneath it with the distinct thread-membership glyph ('└', never the
# relationship glyph), m-fix003 still carrying the unread-to-me marker and
# m-fix004 (e2e-agent's own reply) not.
Type "z"
Type "o"
Wait+Screen@30s />\s+▾\s+●\s+F\s+e2e-agent/
Wait+Screen@30s /●\s+└\s+alice/
Wait+Screen@30s /└\s+e2e-agent\s+alice/

# Shift+V: MailKeyMap.ToggleListModeRequested swaps the list pane from
# threaded rollups to the pre-epic flat per-message stream (kept, not
# deleted, per the epic's non-goals) -- "Workspace (6)", one row per
# message rather than per thread, selection preserved on m-fix004 (the
# thread row's own last message).
Type "V"
Wait+Screen@30s /Workspace \(6\)/

# Shift+S: direct jump to Sent. List mode is an axis independent of
# mailbox, so Sent renders flat too: "Sent (2)", e2e-agent's own two sent
# messages (m-fix004 to alice, m-fix005 to bob), both carrying the from-me
# glyph.
Type "S"
Wait+Screen@30s /Sent \(2\)/
Wait+Screen@30s /F\s+e2e-agent\s+alice/
Wait+Screen@30s /F\s+e2e-agent\s+bob/

# Shift+I: direct jump back to Inbox. Final state for the golden.
Type "I"
Wait+Screen@30s /Inbox \(3\)/

Sleep 1500ms
Screenshot mail-board-flow-final.png
Sleep 500ms
