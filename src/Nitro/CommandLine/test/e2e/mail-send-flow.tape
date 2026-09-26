# End-to-end recording of `nitro agent mail`'s send/inbox/reply round trip in
# a copy of the fixture workspace. The flow runs commands for real so
# `send` and `reply` mint their own message ids.
#
# register two of the fixture's actors at the `nitro agent` root (--actor,
# one invocation each), alice with a --role so the flow demonstrates
# RegisterAgentCommand's
# role and ReadMailCommand's "From: name (role)" header -> alice sends to
# bob, cc'ing itself (--cc) -> bob's inbox shows it unread -> bob
# replies (ReplyMessageAsync flattens the original's sender/to/cc into the
# reply's own "to", deduped, minus the replying actor) -> the message is read
# back with --thread as alice, printing both messages, the first one's
# header showing alice's role.
#
# Neither actor here has a live session bound, so neither send nor reply can
# push a nudge. That is not an error: the mail is stored, the command prints
# "Sent '<id>' to ..." and exits 0, and the recipient sees it on their next
# pull. A missing session only costs the push, never the message.
#
# Every typed command is kept short and a `Sleep 500ms` follows each
# Wait+Screen (mirroring close-reopen-flow.tape's chained-command pattern):
# without it, a real bash PTY's prompt/readline redraw can still be settling
# when the next command starts typing, producing a stray leading character
# that differs between recordings -- non-determinism this tier exists to
# avoid, seen even on short single-line commands during this flow's own
# development. Each Wait pattern must match text only the just-run command
# produces: text already on screen (a typed argument, an earlier command's
# output) satisfies the Wait instantly, and the next Type then races the
# still-pending output. Message ids are minted from wall-clock time
# (MailStore.CreateMessageIdAsync) and dates are real `now`, both
# non-deterministic across recordings; run.sh's mail-send SCRUBS entry
# normalizes both before diffing. Needs no auth: mail commands do not call
# the Nitro backend.
# Produces mail-send-flow.gif + mail-send-flow.txt.

Output mail-send-flow.gif
Output mail-send-flow.txt

# Height is taller than the other flows' shared 850 (see run.sh/other tapes):
# register's identity-only fallback prints an extra "No live session bound"
# line for each of the two registrations here, and send's default wake
# attempt now fails against those same sessionless actors,
# printing "message stored, but wake failed: ..." plus one line per
# recipient instead of a single "Sent" line. At 850 (or 950, before this
# growth) that pushed the tail of the final `mail read --thread` output (the
# second message's Subject/Thread lines) off the bottom of the fixed-size
# terminal, silently dropping it from the golden. 1100 keeps the full output
# on screen.
Set Shell bash
Set FontSize 18
Set Width 1100
Set Height 1100
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
# `nitro agent list` (ListAgentCommand.Order) breaks ties between agents in
# the same state by their last-seen time floored to a clock-aligned 5-minute
# window, then by name (see AgentStateResolver.LastSeenWindowStart). The two
# live `register` calls below each touch last_seen_at to the real wall clock
# at the instant they run, so if they straddle a window boundary (e.g. one
# lands at 10:04:59, the next at 10:05:00) bob's newer window would sort him
# ahead of alice, flipping the two rows the `list` step asserts and failing
# the golden on an unlucky run. Shadow `nitro` with a function that, after
# every real `agent register`, pins both agents' last_seen_at to one
# identical timestamp computed once and written in a single statement, so
# both get the exact same value regardless of the real elapsed time between
# the two registrations, forcing the tie back onto name order every run.
# `command nitro` still resolves the real binary via the PATH exported
# above. The VHS image has no `sqlite3` CLI, only `python3`'s stdlib
# `sqlite3` module; the written format matches the `Microsoft.Data.Sqlite`-
# persisted `DateTimeOffset` text fixtures/agents-seed.sql already uses for
# the same column. This lives in the hidden setup, not the visible demo, so
# `register`'s own printed output (and the recorded frame) is unaffected.
Type `nitro() { command nitro "$@"; local rc=$?; if [ "$1 $2" = "agent register" ]; then python3 -c 'import sqlite3, datetime; now = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%d %H:%M:%S") + ".0000000+00:00"; c = sqlite3.connect(".nitro/agents/agents.db"); c.execute("UPDATE agents SET last_seen_at = ? WHERE name IN (?, ?)", (now, "alice", "bob")); c.commit()'; fi; return $rc; }` Enter
Type `clear` Enter
Show

# --- the demo ---
# The fixture workspace already carries alice and bob as allocated actors:
# names are minted by `agent login` or a session-start hook, never invented,
# so `register` here does what it now does, set a role on an actor that
# already exists.
Type `nitro agent register --actor alice --role reviewer` Enter
Wait+Screen@30s /Actor 'alice', role 'reviewer'/
Sleep 500ms

Type `nitro agent register --actor bob` Enter
Wait+Screen@30s /Actor 'bob'/
Sleep 500ms

# The new unified `agents` table's list shape (ListAgentCommand): one line
# per agent, Name/Role/Harness/Started/Last seen/Online columns. Neither
# actor here has a harness or a live session (register only ever sets role
# and touches last_seen_at), so both are Unreachable ("no"); this step only
# asserts their two rows, not the full listing, since fixtures/agents-
# seed.sql's own Online agent (nora, an Online "yes" row here too) and
# stale-offline bob also show up in this same shared fixture (see that
# file's header for why the Online one is a name with no mail history
# rather than alice or bob).
Type `nitro agent list` Enter
Wait+Screen@30s /alice\s+reviewer\s+-\s+\S+\s+\S+\s+no/
Wait+Screen@30s /bob\s+-\s+-\s+\S+\s+\S+\s+no/
Sleep 500ms

# Send from alice to bob, cc'ing alice itself so the run exercises
# --cc without needing a third registered actor.
Type `nitro agent mail send --to bob --actor alice --cc alice --subject Status --body All-good` Enter
Wait+Screen@30s /Sent '/
Sleep 500ms

Type `nitro agent mail inbox --actor bob` Enter
Wait+Screen@30s /message\(s\)/
Sleep 500ms

# Captures the id from bob's own inbox listing (its first column, see
# MailInboxRow.Format) rather than from the send command's own output.
# This spawns its own nitro process inside the substitution, so it needs a
# longer settle than the single Sleep 500ms every other step here uses:
# typing the next line before this one's inbox+awk pipeline actually
# returns races the still-running foreground command, corrupting the
# prompt redraw (the same class of race the header comment above warns
# about, just with a slower command behind it).
Type `MSG_ID=$(nitro agent mail inbox --actor bob | awk 'NR==1{print $1}')` Enter
Sleep 2000ms
Type `echo "captured $MSG_ID"` Enter
Wait+Screen@30s /captured m-/
Sleep 500ms

Type `nitro agent mail reply --message "$MSG_ID" --actor bob --body Thanks-noted` Enter
Wait+Screen@30s /Sent '[^']+' to alice\./
Sleep 500ms

# Read as alice (the original sender, and a recipient of bob's reply
# once ReplyMessageAsync flattens participants): prints both messages,
# oldest first, marks alice's copy of the reply read, and shows alice's
# "reviewer" role in the first message's From header.
Type `nitro agent mail read --message "$MSG_ID" --thread --actor alice` Enter
Wait+Screen@30s /From: bob[\s\S]*Thanks-noted/

Sleep 1500ms
Screenshot mail-send-flow-final.png
Sleep 500ms
