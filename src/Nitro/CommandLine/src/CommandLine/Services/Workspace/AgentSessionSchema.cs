namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Defines session presence, per-channel delivery reservations, and the four
/// workspace ping-lease slots.
/// </summary>
internal static class AgentSessionSchema
{
    /// <summary>
    /// The columns and constraints of the session presence table.
    /// </summary>
    private const string AgentSessionsColumns =
        """
            harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot', 'opencode', 'nitro-board')),
            session_id TEXT NOT NULL,
            agent_name TEXT NULL REFERENCES agents (name),
            binding_kind TEXT NOT NULL DEFAULT 'none' CHECK (binding_kind IN ('none', 'env', 'explicit')),
            host TEXT NOT NULL,
            cwd TEXT NOT NULL,
            workspace_path TEXT NOT NULL,
            endpoint_kind TEXT NOT NULL CHECK (endpoint_kind IN ('claude-peer', 'codex-thread', 'copilot-extension', 'opencode-server', 'db-watch', 'none')),
            endpoint_addr TEXT NOT NULL,
            endpoint_secret TEXT NULL,
            started_at TEXT NOT NULL,
            last_beat_at TEXT NOT NULL,
            block_budget_used INTEGER NOT NULL DEFAULT 0 CHECK (block_budget_used >= 0),
            last_ping_at TEXT NULL,
            last_ping_attempt TEXT NULL,
            last_ping_result TEXT NULL CHECK (last_ping_result IN ('ok', 'spawn-failed', 'endpoint-gone', 'timeout', 'capacity-dropped', 'error', 'unsupported') OR last_ping_result IS NULL),
            last_ping_detail TEXT NULL CHECK (last_ping_detail IS NULL OR length(last_ping_detail) <= 200),
            role TEXT NOT NULL DEFAULT '',
            harness_version TEXT NOT NULL DEFAULT '',
            announcement_pending INTEGER NOT NULL DEFAULT 0 CHECK (announcement_pending IN (0, 1)),
            idle_push_armed INTEGER NOT NULL DEFAULT 0 CHECK (idle_push_armed IN (0, 1)),
            CHECK ((binding_kind = 'none') = (agent_name IS NULL)),
            CHECK ((endpoint_kind = 'none') = (endpoint_addr = '')),
            PRIMARY KEY (harness, session_id)
        """;

    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS agent_sessions (
        """
        + AgentSessionsColumns
        + """

        );

        CREATE INDEX IF NOT EXISTS idx_agent_sessions_name ON agent_sessions (agent_name);

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

    /// <summary>
    /// Returns SQL to create the session presence table under <paramref name="tableName"/>.
    /// </summary>
    public static string CreateAgentSessionsTable(string tableName) =>
        $"""
        CREATE TABLE "{tableName}" (
        {AgentSessionsColumns}
        );
        """;
}
