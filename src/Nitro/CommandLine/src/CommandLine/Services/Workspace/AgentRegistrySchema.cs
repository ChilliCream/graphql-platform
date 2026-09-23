namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal static class AgentRegistrySchema
{
    /// <summary>
    /// Creates the unified agents table if it does not exist; existing tables are unchanged.
    /// Carries the transitional <c>implicit</c> and <c>client</c> columns the old
    /// <see cref="AgentRegistry"/> still reads; a later task drops them.
    /// </summary>
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS agents (
            name TEXT PRIMARY KEY,
            role TEXT NOT NULL DEFAULT '',
            harness TEXT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot', 'opencode')),
            harness_version TEXT NOT NULL DEFAULT '',
            session_id TEXT NULL,
            cwd TEXT NOT NULL DEFAULT '',
            workspace_path TEXT NOT NULL DEFAULT '',
            registered_at TEXT NOT NULL,
            started_at TEXT NOT NULL,
            last_seen_at TEXT NOT NULL,
            ended_at TEXT NULL,
            deleted_at TEXT NULL,
            endpoint_kind TEXT NOT NULL DEFAULT 'none' CHECK (endpoint_kind IN ('claude-peer', 'codex-thread', 'copilot-extension', 'opencode-server', 'db-watch', 'none')),
            endpoint_addr TEXT NOT NULL DEFAULT '',
            endpoint_secret TEXT NULL,
            block_budget_used INTEGER NOT NULL DEFAULT 0,
            last_ping_at TEXT NULL,
            last_ping_attempt TEXT NULL,
            last_ping_result TEXT NULL CHECK (last_ping_result IN ('ok', 'spawn-failed', 'endpoint-gone', 'timeout', 'capacity-dropped', 'error', 'unsupported') OR last_ping_result IS NULL),
            last_ping_detail TEXT NULL CHECK (last_ping_detail IS NULL OR length(last_ping_detail) <= 200),
            announcement_pending INTEGER NOT NULL DEFAULT 0,
            idle_push_armed INTEGER NOT NULL DEFAULT 0,
            implicit INTEGER NOT NULL DEFAULT 0 CHECK (implicit IN (0, 1)),
            client TEXT NOT NULL DEFAULT ''
        );

        CREATE UNIQUE INDEX IF NOT EXISTS idx_agents_session ON agents (harness, session_id) WHERE harness IS NOT NULL;
        """;
}
