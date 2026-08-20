# End-to-end recording of `nitro agent mail`'s send/inbox/reply round trip in
# a fresh, live mail workspace (no fixture; unlike mail-board-flow, this flow
# needs commands to run for real so `send`/`reply` mint their own message
# ids).
#
# init -> register two actors (--actor, one invocation each; the persistent
# NITRO_MAIL_ACTOR env var form is exercised by mail-board-flow instead) ->
# agent-a sends to agent-b, cc'ing itself (--cc) -> agent-b's inbox shows it
# unread -> agent-b replies (ReplyMessageAsync flattens the original's
# sender/to/cc into the reply's own "to", deduped, minus the replying actor)
# -> the message is read back with --thread as agent-a, printing both
# messages.
#
# Every typed command is kept short and a `Sleep 500ms` follows each
# Wait+Screen (mirroring close-reopen-flow.tape's chained-command pattern):
# without it, a real bash PTY's prompt/readline redraw can still be settling
# when the next command starts typing, producing a stray leading character
# that differs between recordings -- non-determinism this tier exists to
# avoid, seen even on short single-line commands during this flow's own
# development. Message ids are minted from wall-clock time
# (MailStore.CreateMessageIdAsync) and dates are real `now`, both
# non-deterministic across recordings; run.sh's mail-send SCRUBS entry
# normalizes both before diffing. Needs no auth: mail commands do not call
# the Nitro backend.
# Produces mail-send-flow.gif + mail-send-flow.txt.

Output mail-send-flow.gif
Output mail-send-flow.txt

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
Type `rm -rf /tmp/work && mkdir -p /tmp/work && cd /tmp/work` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `nitro agent mail init` Enter
Wait+Screen@30s /Initialized mail workspace/
Sleep 500ms

Type `nitro agent mail register --actor agent-a` Enter
Wait+Screen@30s /Registered 'agent-a'/
Sleep 500ms

Type `nitro agent mail register --actor agent-b` Enter
Wait+Screen@30s /Registered 'agent-b'/
Sleep 500ms

# Send from agent-a to agent-b, cc'ing agent-a itself so the run exercises
# --cc without needing a third registered actor.
Type `nitro agent mail send agent-b --actor agent-a --cc agent-a --subject Status --body All-good` Enter
Wait+Screen@30s /Sent '/
Sleep 500ms

Type `nitro agent mail inbox --actor agent-b` Enter
Wait+Screen@30s /Status/
Sleep 500ms

# Captures the id from agent-b's own inbox listing (its first column, see
# MailInboxRow.Format) rather than from the send command's own output.
Type `MSG_ID=$(nitro agent mail inbox --actor agent-b | awk 'NR==1{print $1}')` Enter
Sleep 500ms
Type `echo "captured $MSG_ID"` Enter
Wait+Screen@30s /captured m-/
Sleep 500ms

Type `nitro agent mail reply "$MSG_ID" --actor agent-b --body Thanks-noted` Enter
Wait+Screen@30s /Sent '/
Sleep 500ms

# Read as agent-a (the original sender, and a recipient of agent-b's reply
# once ReplyMessageAsync flattens participants): prints both messages,
# oldest first, and marks agent-a's copy of the reply read.
Type `nitro agent mail read "$MSG_ID" --thread --actor agent-a` Enter
Wait+Screen@30s /Thanks-noted/

Sleep 1500ms
Screenshot mail-send-flow-final.png
Sleep 500ms
