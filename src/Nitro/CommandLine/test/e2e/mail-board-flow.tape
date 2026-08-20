# End-to-end recording of `nitro agent mail board`, keystroke-driven, against
# the seeded mail fixture (see fixtures/mail-seed.sql), mirroring
# board-flow.tape's structure for the task board.
#
# The mail board TUI (raw-key event loop, full-screen alternate-screen
# rendering) cannot be exercised by the in-process test tier; this is the
# VHS tier's job. Drives row selection (Down) to the fixture's oldest inbox
# message, opens its detail pane (Enter, also marking it read), then toggles
# the thread view ('t') to reveal its reply -- unread styling, the detail
# pane, and the thread toggle, the three things this flow is scoped to per
# the mail e2e tape bead (not exhaustive per-command coverage, that is the
# unit tier's job).
#
# fixtures/mail-seed.sql gives e2e-agent three inbox messages (it is a `to`
# recipient of each): m-fix002 (bob, already read, newest by created_at),
# m-fix001 (alice, unread), m-fix003 (alice, unread, the root of a thread
# whose reply, m-fix004, is e2e-agent's own and so never appears in the
# inbox list itself, only via the thread toggle). Every seeded `created_at`
# is far enough in the past that MailAges' age column always renders as a
# fixed "yyyy-MM-dd" string, independent of the wall-clock date a recording
# actually runs on, so this flow needs no SCRUBS entry (unlike
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
Type `rm -rf /tmp/work && mkdir -p /tmp/work && cp -r /vhs/fixture/mailbox /tmp/work/mailbox && cd /tmp/work/mailbox` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro agent mail board` Enter

# Initial frame: list pane titled "Inbox (3)", selection on the first row
# (m-fix002, bob, newest, already read).
Wait+Screen@30s /Inbox \(3\)/

# Down, Down: onto the third row (m-fix003, alice, unread -- the thread
# root). The unread marker ('*') sits directly before the sender name
# regardless of the row's exact selection/subject rendering, so the wait
# anchors on marker+sender rather than the (width-truncatable) subject.
Down
Down
Wait+Screen@30s /> \* alice /

# Enter: focus the detail pane for the selected message (also marks it read
# for e2e-agent).
Enter
Wait+Screen@30s /From: alice/

# 't': toggle the detail pane from the single message to its whole thread,
# revealing e2e-agent's own reply (m-fix004), which is never listed in the
# inbox itself. Final state for the golden.
Type "t"
Wait+Screen@30s /Thanks, looks good to me\./

Sleep 1500ms
Screenshot mail-board-flow-final.png
Sleep 500ms
