# End-to-end recording of the Agents tab: the registered agent list next to
# its detail pane, keystroke-driven, against the deterministic fixture
# workspace (fixtures/seed.sql + fixtures/mail-seed.sql + fixtures/agents-seed.sql,
# all in the one out/fixture/acme agents.db), mirroring board-flow.tape's/
# mail-board-flow.tape's structure.
#
# Launched via bare `nitro agent`, landing on the Tasks tab first (the same
# first frame board-flow.tape and mail-board-flow.tape both assert), then
# Shift+A (TabSwitchKeys.ResolveMnemonic, the tab strip's [A]gents mnemonic)
# jumps straight to Agents.
#
# Presence badges use one glyph per AgentRowBadge.PresenceGlyph: offline,
# online, unreachable, or remote. The list is built from AgentsState.RefreshAsync
# calling AgentSessionRegistry.ListParticipantsAsync, one row per live
# agent_sessions row (harness, session id), not one row per registered
# agent: an agent with no session row simply has no participant row and so
# never appears in the tab at all, offline or otherwise. The fixture seeds
# exactly one session (bob, remote); alice and e2e-agent, which have no
# session row, are absent from the list entirely (fixtures/agents-seed.sql's
# header explains the remote-vs-live distinction that made bob the one state
# a static SQL fixture can pin byte-stably).
#
# perles-net-nhs (feat(nitro-agent): register both board entry points as
# live operator sessions) starts a live nitro-board session for bare
# `nitro agent` itself, before the shell even opens, so the tab can show a
# second row for the board alongside the seeded one. ListParticipantsAsync
# orders by harness then session id, and "claude-code" sorts ahead of
# "nitro-board", so bob stays row 0 and selected by default; the board's own
# row lands second.
# That row's fields are all deterministic except its two ages: actor
# e2e-agent (the resolved mail actor), harness nitro-board, state online
# (its host always matches the current instance, and the process is,
# definitionally, alive). Started/Last heard are MailAges.Format's relative
# ages, computed against wall-clock time; the board session starts moments
# before this frame is captured, so they read "now" in a fast run but can
# drift to "{n}m" if recording is slow, so run.sh's SCRUBS[agents] entry
# normalizes both to AGE before diffing. Its role (operator,
# BoardSessionLifecycle.OperatorRole) is set the same way, but
# AgentRowBadge.Render truncates the role column to
# nothing at this tape's fixed 1100-wide geometry once a second, wider
# ("nitro-board") harness value is on screen - the same truncation bob's
# own row already carries in the pre-existing golden, role "-" included.
# Confirmed against a live run of the published binary, not guessed from
# source.
#
# Drives: land on Agents, two rows, the fixture's bob (remote) selected by
# default; its detail pane shows the new Session section (session id, host,
# started/last-heard) ahead of Identity, then Sent mail (m-fix006, shared
# with the mail board tape's own fixture data); no Tasks section, since bob
# is nobody's task assignee. The board's own row is asserted from the list
# line alone (actor, presence glyph, harness) - its detail pane is never
# opened, so the real session id/host/pid a live process carries never
# needs to be pinned.
#
# Produces agents-flow.gif + agents-flow.txt.

Output agents-flow.gif
Output agents-flow.txt

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

# Initial frame: the Tasks tab's board (same sentinel as board-flow.tape's
# and mail-board-flow.tape's first Wait+Screen).
Wait+Screen@30s /> ○ \[B\] P0 acme-a1b Checkout crashes on Safari/

# Shift+A: TabSwitchKeys.ResolveMnemonic jumps straight to the Agents tab.
# Two live agent_sessions rows now exist (fixture-seeded bob, plus this
# launch's own live board session), sorted harness-then-session-id: bob
# (claude-code) stays row 0 and selected by default, the board session
# (nitro-board, actor e2e-agent, role operator, state online) is row 1.
Type "A"
Wait+Screen@30s /Agents \(2\)/
Wait+Screen@30s /─bob─/
Wait+Screen@30s /bob\s+◇/
Wait+Screen@30s /e2e-agent\s+●\s+nitro-board/
Wait+Screen@30s /Session/
Wait+Screen@30s /Host: e2e-remote-host-fixture/

# bob is nobody's task assignee, so the Tasks section is omitted entirely
# (AgentDetailBody.Build drops an empty section, header included) - the
# detail pane goes straight from Identity to Sent mail.
Wait+Screen@30s /Name: bob/
Wait+Screen@30s /Billing review/

Sleep 1500ms
Screenshot agents-flow-final.png
Sleep 500ms
