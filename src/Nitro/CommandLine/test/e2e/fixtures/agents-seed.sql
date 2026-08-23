-- Deterministic agent_sessions fixture for the agents-flow e2e tape (the
-- Agents tab's registered agent list with presence badges).
--
-- Applied by run.sh after mail-seed.sql (agent_name references agents.name,
-- which mail-seed.sql populates) against the same unified agent workspace
-- database seed.sql/mail-seed.sql seed. See fixtures/README.md.
--
-- AgentSessionRegistry.ListAsync resolves presence from a session row's
-- `host` column against the CURRENT machine's own instance id
-- (NitroInstanceIdProvider, a hash of /etc/machine-id or a generated
-- fallback): a row whose host differs from that id renders `Remote` (`◇`)
-- unconditionally, with no PID-liveness check at all, since
-- AgentSessionRegistry.ReapAsync only ever reaps rows matching the CURRENT
-- host. `Online`/`Unreachable`, by contrast, both require ReapAsync's
-- IsAlive(pid, proc_start) check to pass against the CURRENT host's real
-- process table, which cannot be faked from a static SQL fixture applied
-- before any tape process exists - so this fixture seeds exactly one Remote
-- session (bob) and leaves e2e-agent/alice with none, rendering `Offline`
-- (`○`, the default for an agent with zero session rows). Together the two
-- states are what the agents-flow tape asserts on the presence badge
-- column; `host` below is an arbitrary string that can never coincidentally
-- equal a real machine's hashed instance id or generated fallback GUID.

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
