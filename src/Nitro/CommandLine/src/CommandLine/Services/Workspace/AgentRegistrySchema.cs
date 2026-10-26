namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal static class AgentRegistrySchema
{
    private const string TableDefinition =
        """
        (
            name TEXT PRIMARY KEY,
            role TEXT NOT NULL DEFAULT '',
            harness TEXT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot', 'opencode', 'venti')),
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
            idle_push_armed INTEGER NOT NULL DEFAULT 0
        )
        """;

    private const string ColumnNames =
        """
        name, role, harness, harness_version, session_id, cwd, workspace_path,
        registered_at, started_at, last_seen_at, ended_at, deleted_at,
        endpoint_kind, endpoint_addr, endpoint_secret, block_budget_used,
        last_ping_at, last_ping_attempt, last_ping_result, last_ping_detail,
        announcement_pending, idle_push_armed
        """;

    private const string CreateSessionIndex =
        """
        CREATE UNIQUE INDEX IF NOT EXISTS idx_agents_session ON agents (harness, session_id) WHERE harness IS NOT NULL;
        """;

    /// <summary>
    /// Creates the unified agents table if it does not exist; existing tables are unchanged.
    /// </summary>
    public const string Create =
        $"""
        CREATE TABLE IF NOT EXISTS agents {TableDefinition};

        {CreateSessionIndex}
        """;

    /// <summary>
    /// Replaces an agents table that already has every current column with one carrying the
    /// current constraints, copying every row unchanged. Requires foreign key enforcement to be
    /// off, so tables referencing <c>agents (name)</c> keep their rows and reference the replacement.
    /// </summary>
    public const string Rebuild =
        $"""
        CREATE TABLE agents_rebuild {TableDefinition};

        INSERT INTO agents_rebuild ({ColumnNames})
        SELECT {ColumnNames} FROM agents;

        DROP TABLE agents;

        ALTER TABLE agents_rebuild RENAME TO agents;

        {CreateSessionIndex}
        """;
}
