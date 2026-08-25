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
-- unconditionally, with no PID-liveness check at all, since
-- AgentSessionRegistry.ReapAsync only ever reaps rows matching the CURRENT
-- host. `Online`/`Unreachable`, by contrast, both require ReapAsync's
-- IsAlive(pid, proc_start) check to pass against the CURRENT host's real
-- process table, which cannot be faked from a static SQL fixture applied
-- before any tape process exists, so this fixture seeds exactly one Remote
-- session (bob), the only state a static fixture can pin byte-stably;
-- `host` below is an arbitrary string that can never coincidentally equal a
-- real machine's hashed instance id or generated fallback GUID.

BEGIN TRANSACTION;

INSERT INTO agent_sessions (
    harness, session_id, agent_name, binding_kind, host, pid, proc_start,
    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at
) VALUES (
    'claude-code', 'e2e-remote-session', 'bob', 'explicit', 'e2e-remote-host-fixture', 424242,
    '2026-01-01 06:00:00.0000000+00:00', '/tmp/remote-cwd', '/tmp/remote-workspace',
    'none', '', '2026-01-01 06:00:00.0000000+00:00', '2026-01-01 06:00:00.0000000+00:00'
);

COMMIT;
