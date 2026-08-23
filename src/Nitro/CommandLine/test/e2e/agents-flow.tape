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
# via its comment 64). AgentSessionRegistry.ListAsync only
# derives online/unreachable from a real, live process matching the
# fixture's own machine and PID at read time, which a static SQL fixture
# applied before any tape process runs cannot fake; remote, by contrast,
# needs only a session row whose host differs from the current machine's
# resolved instance id, with no liveness check at all (fixtures/agents-seed.sql's
# header explains why). This tape therefore covers exactly the two states a
# static fixture CAN pin byte-stably: offline (the default for an agent with
# no session row: alice, e2e-agent) and remote (bob, via the seeded session).
#
# Drives: land on Agents, selection on the alphabetically-first row (alice,
# offline, AgentRegistry.ListAsync's own ORDER BY name), Identity/Tasks/Sent
# mail all populated in the detail pane; Down onto bob (remote), whose
# detail pane has no Tasks section (unassigned) but does have Sent mail
# (m-fix006, shared with the mail board tape's own fixture data).
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
# Selection defaults to row 0, alphabetically-first (alice), offline (no
# session row in the fixture).
Type "A"
Wait+Screen@30s /Agents \(3\)/
Wait+Screen@30s /alice\s+○/
Wait+Screen@30s /Identity/
Wait+Screen@30s /Name: alice/
Wait+Screen@30s /Tasks/
Wait+Screen@30s /Draft new pricing/

# Down: onto bob, the fixture's one remote agent (fixtures/agents-seed.sql).
# Unlike alice, bob is nobody's task assignee, so the Tasks section is
# omitted entirely (AgentDetailBody.Build drops an empty section, header
# included) - the detail pane goes straight from Identity to Sent mail.
Down
Wait+Screen@30s /─bob─/
Wait+Screen@30s /bob\s+◇/
Wait+Screen@30s /Name: bob/
Wait+Screen@30s /Billing review/

Sleep 1500ms
Screenshot agents-flow-final.png
Sleep 500ms
