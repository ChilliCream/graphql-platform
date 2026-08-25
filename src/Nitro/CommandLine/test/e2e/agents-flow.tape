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
# perles-net-k3j.18 shipped presence badges in the list row (a glyph per
# AgentRowBadge.PresenceGlyph: offline, online, unreachable, remote)
# but no e2e tape ever exercised the Agents tab at all (that ticket's own
# assumption otherwise was wrong; authoring one was routed to perles-net-02r
# via its comment 64). The list is now built from AgentsState.RefreshAsync
# calling AgentSessionRegistry.ListParticipantsAsync, one row per live
# agent_sessions row (harness, session id), not one row per registered
# agent: an agent with no session row simply has no participant row and so
# never appears in the tab at all, offline or otherwise. The fixture seeds
# exactly one session (bob, remote), so the tab shows exactly one row.
# alice and e2e-agent, which have no session row, are absent from the list
# entirely (fixtures/agents-seed.sql's header explains the remote-vs-live
# distinction that made bob the one state a static SQL fixture can pin
# byte-stably).
#
# Drives: land on Agents, the fixture's one row (bob, remote) selected by
# default; its detail pane shows the new Session section (session id, host,
# started/last-heard) ahead of Identity, then Sent mail (m-fix006, shared
# with the mail board tape's own fixture data); no Tasks section, since bob
# is nobody's task assignee.
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
Type `export NITRO_TASK_ACTOR=e2e-agent` Enter
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
# The fixture seeds exactly one agent_sessions row (bob, remote), so the
# list holds exactly one row and it is selected by default.
Type "A"
Wait+Screen@30s /Agents \(1\)/
Wait+Screen@30s /─bob─/
Wait+Screen@30s /bob\s+◇/
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
