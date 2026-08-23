namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Schema v4: agent presence and mail delivery/ping bookkeeping, added
/// alongside the identity table in <see cref="AgentRegistrySchema"/>. Three
/// tables: <c>agent_sessions</c> (one row per live harness session, claimed
/// or not, keyed by (harness, session_id)); <c>session_deliveries</c> (the
/// at-most-once-per-channel notification ledger, cascading with its owning
/// session); and <c>ping_leases</c> (the fixed four-slot concurrency cap on
/// outstanding ping children). Statements are idempotent so applying them to
/// an existing database is non-destructive. <c>last_ping_result</c> carries
/// <c>unsupported</c> for an endpoint kind the notifier has no transport
/// for (<c>claude-peer</c>, currently): a distinct diagnostic from
/// <c>endpoint_kind = 'none'</c>, which means the session simply has no
/// endpoint to attempt at all.
/// </summary>
internal static class AgentSessionSchema
{
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS agent_sessions (
            harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot')),
            session_id TEXT NOT NULL,
            agent_name TEXT NULL REFERENCES agents (name),
            binding_kind TEXT NOT NULL DEFAULT 'none' CHECK (binding_kind IN ('none', 'env', 'explicit')),
            host TEXT NOT NULL,
            pid INTEGER NOT NULL CHECK (pid > 0),
            proc_start TEXT NOT NULL,
            cwd TEXT NOT NULL,
            workspace_path TEXT NOT NULL,
            endpoint_kind TEXT NOT NULL CHECK (endpoint_kind IN ('claude-peer', 'codex-thread', 'copilot-extension', 'none')),
            endpoint_addr TEXT NOT NULL,
            started_at TEXT NOT NULL,
            last_beat_at TEXT NOT NULL,
            block_budget_used INTEGER NOT NULL DEFAULT 0 CHECK (block_budget_used >= 0),
            last_ping_at TEXT NULL,
            last_ping_attempt TEXT NULL,
            last_ping_result TEXT NULL CHECK (last_ping_result IN ('ok', 'spawn-failed', 'endpoint-gone', 'timeout', 'capacity-dropped', 'error', 'unsupported') OR last_ping_result IS NULL),
            last_ping_detail TEXT NULL CHECK (last_ping_detail IS NULL OR length(last_ping_detail) <= 200),
            -- Table-level CHECK constraints must follow every column
            -- definition (SQLite rejects one interleaved between columns),
            -- so both cross-column checks live here instead of next to the
            -- columns they compare.
            CHECK ((binding_kind = 'none') = (agent_name IS NULL)),
            CHECK ((endpoint_kind = 'none') = (endpoint_addr = '')),
            PRIMARY KEY (harness, session_id)
        );

        CREATE INDEX IF NOT EXISTS idx_agent_sessions_name ON agent_sessions (agent_name);
        CREATE INDEX IF NOT EXISTS idx_agent_sessions_pid ON agent_sessions (host, pid);

        CREATE TABLE IF NOT EXISTS session_deliveries (
            harness TEXT NOT NULL,
            session_id TEXT NOT NULL,
            message_id TEXT NOT NULL,
            channel TEXT NOT NULL CHECK (channel IN ('digest', 'gate', 'ping')),
            delivered_at TEXT NOT NULL,
            PRIMARY KEY (harness, session_id, message_id, channel),
            FOREIGN KEY (harness, session_id)
                REFERENCES agent_sessions (harness, session_id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS ping_leases (
            slot INTEGER PRIMARY KEY CHECK (slot BETWEEN 1 AND 4),
            attempt_id TEXT NOT NULL,
            acquired_at TEXT NOT NULL,
            expires_at TEXT NOT NULL
        );
        """;
}
