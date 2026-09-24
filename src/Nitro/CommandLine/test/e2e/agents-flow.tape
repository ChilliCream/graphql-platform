# End-to-end recording of the Agents tab: the unified agent table, its
# presence bubbles and header counts, and the Enter detail popover, against
# the deterministic fixture workspace (fixtures/seed.sql + mail-seed.sql +
# agents-seed.sql, all in the one out/fixture/acme agents.db), mirroring
# board-flow.tape's/mail-board-flow.tape's structure.
#
# Launched via bare `nitro agent`, landing on the Tasks tab first (the same
# first frame board-flow.tape and mail-board-flow.tape both assert), then
# Shift+A (TabSwitchKeys.ResolveMnemonic, the tab strip's [A]gents mnemonic)
# jumps straight to Agents.
#
# AgentsMode renders every non-deleted agent as one full-width table
# (AgentRowBadge), sorted Online, then Unreachable, then Offline
# (AgentsState.ApplyFilterAndSort). fixtures/agents-seed.sql gives nora that
# Online state (a role, a harness, a live session, a non-'none' endpoint),
# alice Unreachable (no harness/session, recently seen), and bob/e2e-agent
# both Offline (bob stale, e2e-agent ended_at set); wren is soft-deleted and
# so never appears in the table's "1 online / 4" header count either (see
# that file's own header for why these four states land on these four
# names, and why the Online one specifically is a fresh name rather than
# one of mail-seed.sql's three message participants).
#
# nora is Online and sorts first, so she is also the row Enter opens the
# popover on: AgentPopoverModel loads her identity plus her mail, ticket,
# and memory participation (AgentPopoverView). Her Mail and Tickets
# sections are both empty (she is not a party to any seeded message or
# task), so the cursor starts on Mail's own "show more" row
# (AgentPopoverModel.Locate: an empty section's show-more row is not
# preceded by any item rows) and j walks it past Tickets' equally-empty
# show-more row onto her three seeded memory_journal entries
# (fixtures/agents-seed.sql), listed most recent first. Further j presses
# move the selection down to the third (oldest) entry and onto Memory's
# own "show more" row; Enter there opens its full-list view
# (AgentPopoverListMode), rendering all three entries, and j/k move the
# selection inside that list, exercising both the section-to-section
# cursor and the show-more list's own scrolling. The first Escape backs
# out of that list to the summary popover, and the second Escape closes
# the popover entirely, returning to the table. From there, Shift+D
# presses the table's own delete-offline binding (AgentsKeyMap's D,
# distinct from the popover's per-agent 'd'), opening the confirmation
# TuiShell.TryOpenAgentDeleteOfflineDialog shows for the currently Offline
# agents, bob and e2e-agent ("Delete 2 offline agents? This cannot be
# undone."). A final Escape cancels it: TuiShell's Cancelled branch just
# clears the dialog, so the final frame is the table itself, unchanged.
#
# nora's last_seen_at is refreshed to the real current time by
# agents-seed.sql rather than pinned to a fixture-past timestamp (the state
# this table renders is a live, time-windowed computation with no
# host/PID escape hatch to pin it statically the way the old
# agent_sessions model's `Remote` state could be); the formatted age this
# produces ("now"/"Nm ago") is normalized by run.sh's own `agents` SCRUBS
# entry before the diff, since it grows with however long a full `run.sh`
# takes to reach this, the last-recorded flow.
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
Type "A"
Wait+Screen@30s /Agents \(1 online \/ 4\)/

# Sorted Online first: nora (role, harness) is the selected top row.
Wait+Screen@30s /nora\s+planner\s+Claude Code/

# Enter opens the detail popover for the selected (Online) agent.
Enter
Wait+Screen@30s /State: ● Online/
Wait+Screen@30s /Role: planner/
Wait+Screen@30s /Harness: claude-code 2\.4\.1/
Wait+Screen@30s /Session id: e2e-online-session/

# The cursor starts on Mail's own "show more" row (nora has no mail at
# all, so that row is the first selectable one). j moves it past the
# equally-empty Tickets show-more row onto Memory's three seeded journal
# entries, most recent first.
Type "j"
Type "j"
Wait+Screen@30s /journal\s+2026-01-01\s+Fusion warnings are non-fatal\./

# j walks the cursor down through the second entry to the third (oldest).
Type "j"
Type "j"

# j once more lands on Memory's own "show more" row; Enter opens its full
# list view.
Type "j"
Enter
Wait+Screen@30s /nora — Memory/
Wait+Screen@30s /Prefer small PRs over big changes\./

# j/k move the selection inside the full list itself.
Type "j"
Type "k"

# Esc backs out of the show-more list to the summary popover.
Escape
Wait+Screen@30s /State: ● Online/

# Esc again closes the popover entirely, back on the table.
Escape
Wait+Screen@30s /Agents \(1 online \/ 4\)/

# Shift+D opens the table-level delete-offline confirmation (distinct from
# the popover's per-agent 'd'), counting bob and e2e-agent; Esc cancels it,
# returning to the table.
Type "D"
Wait+Screen@30s /Delete 2 offline agents\? This cannot be undone\./
Escape
Wait+Screen@30s /Agents \(1 online \/ 4\)/
Wait+Screen@30s /> ● nora/

Sleep 1500ms
Screenshot agents-flow-final.png
Sleep 500ms
