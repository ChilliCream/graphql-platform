-- Deterministic agent_sessions fixture for the agents-flow e2e tape (the
-- Agents tab's live participant list with presence badges).
--
-- Applied by run.sh after mail-seed.sql (agent_name references agents.name,
-- which mail-seed.sql populates) against the same unified agent workspace
-- database seed.sql/mail-seed.sql seed. See fixtures/README.md.
--
-- AgentsState.RefreshAsync builds the tab's rows from
-- AgentSessionRegistry.ListParticipantsAsync, which selects from
-- agent_sessions only, one row per live session, not one row per
-- registered agent. An agent with no session row (e2e-agent, alice in this
-- fixture) is therefore not a participant at all and never appears in the
-- tab, offline or otherwise; the old per-registered-agent `Offline` (`○`)
-- state no longer applies here.
--
-- For the one session row this fixture does seed, presence still resolves
-- from the row's `host` column against the CURRENT machine's own instance
-- id (NitroInstanceIdProvider, a hash of /etc/machine-id or a generated
-- fallback): a row whose host differs from that id renders `Remote` (`◇`)
-- unconditionally and is never reaped, since AgentSessionRegistry.ReapAsync
-- only ever reaps rows matching the CURRENT host. That also keeps this row
-- stable against the heartbeat staleness rule reaping now uses: a
-- last_beat_at this old would be swept immediately on the current host.
-- `host` below is an arbitrary string that can never coincidentally equal a
-- real machine's hashed instance id or generated fallback GUID.

BEGIN TRANSACTION;

INSERT INTO agent_sessions (
    harness, session_id, agent_name, binding_kind, host,
    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
) VALUES (
    'claude-code', 'e2e-remote-session', 'bob', 'explicit', 'e2e-remote-host-fixture',
    '/tmp/remote-cwd', '/tmp/remote-workspace',
    'none', '', '2026-01-01 06:00:00.0000000+00:00', '2026-01-01 06:00:00.0000000+00:00'
);

-- The durable identity row for the same session. `nitro agent list` joins
-- agent_session_identities to the live agent_sessions row by
-- (harness, session_id), so a session seeded without its identity is listed
-- as "no session" no matter what agent_sessions holds.
INSERT INTO agent_session_identities (
    harness, session_id, actor, role, actor_revision, created_at, last_seen_at
) VALUES (
    'claude-code', 'e2e-remote-session', 'bob', '', 1,
    '2026-01-01 06:00:00.0000000+00:00', '2026-01-01 06:00:00.0000000+00:00'
);

COMMIT;
