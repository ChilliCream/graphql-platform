namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// <c>session_ping_gates</c>: the per-session-generation mutual exclusion
/// gate a caller reserves before attempting any endpoint transport against
/// one exact <see cref="AgentSessionGeneration"/>, keyed by its full
/// (harness, session_id, host) tuple. Distinct from <c>ping_leases</c>,
/// which caps total outstanding ping children workspace-wide regardless of
/// which session they target. Not referenced by a foreign key against
/// <c>agent_sessions</c>, so ending or reaping the session it names never
/// implicitly frees or blocks the gate. <c>attempt_id</c> fences release
/// the same way <c>ping_leases.attempt_id</c> does, and an expired gate is
/// reclaimed by stealing it, not by a separate sweep. Statements are
/// idempotent so applying them to an existing database is non-destructive.
/// </summary>
internal static class SessionPingGateSchema
{
    /// <summary>
    /// The <c>session_ping_gates</c> column and constraint list, shared
    /// between <see cref="Create"/> (applied under the live table name) and
    /// <see cref="CreateSessionPingGatesTable"/> (applied under a temporary
    /// name to rebuild the table).
    /// </summary>
    private const string SessionPingGatesColumns =
        """
            harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot', 'opencode', 'nitro-board')),
            session_id TEXT NOT NULL,
            host TEXT NOT NULL,
            attempt_id TEXT NOT NULL,
            acquired_at TEXT NOT NULL,
            expires_at TEXT NOT NULL,
            PRIMARY KEY (harness, session_id, host)
        """;

    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS session_ping_gates (
        """
        + SessionPingGatesColumns
        + """

        );

        CREATE INDEX IF NOT EXISTS idx_session_ping_gates_expires ON session_ping_gates (expires_at);
        """;

    /// <summary>
    /// The same <c>session_ping_gates</c> column and constraint list as
    /// <see cref="Create"/>, applied under <paramref name="tableName"/>
    /// instead of the live table name.
    /// </summary>
    public static string CreateSessionPingGatesTable(string tableName) =>
        $"""
        CREATE TABLE "{tableName}" (
        {SessionPingGatesColumns}
        );
        """;
}
