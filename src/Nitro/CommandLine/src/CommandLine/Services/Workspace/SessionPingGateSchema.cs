namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Schema v7: <c>session_ping_gates</c>, the per-session-generation mutual
/// exclusion gate a caller reserves before attempting any endpoint transport
/// against one exact <see cref="AgentSessionGeneration"/>, keyed by its full
/// (harness, session_id, host, pid, proc_start) tuple so a stale generation
/// (an older pid the OS has since reused, or a superseded SessionStart) can
/// never contend with the current one. Distinct from <c>ping_leases</c>,
/// which caps total outstanding ping children workspace-wide regardless of
/// which session they target; this table instead guarantees at most one
/// attempt in flight against any single session generation at a time. Not
/// referenced by a foreign key against <c>agent_sessions</c>: a gate row is
/// independent of that table's mutable, cascading lifecycle, so ending or
/// reaping the session it names never implicitly frees or blocks the gate.
/// <c>attempt_id</c> fences release the same way <c>ping_leases.attempt_id</c>
/// does, and an expired gate is reclaimed by stealing it, not by a separate
/// sweep. Statements are idempotent so applying them to an existing
/// database is non-destructive. v8 adds <c>nitro-board</c> to the
/// <c>harness</c> CHECK constraint, alongside the same addition to
/// <c>agent_sessions.harness</c>, so a board session can reserve a gate.
/// </summary>
internal static class SessionPingGateSchema
{
    /// <summary>
    /// The <c>session_ping_gates</c> column and constraint list, shared
    /// between <see cref="Create"/> (applied under the live table name) and
    /// <see cref="CreateSessionPingGatesTable"/> (applied by
    /// <see cref="AgentDatabase"/> under a temporary name to rebuild the
    /// table for a database whose <c>harness</c> CHECK constraint predates
    /// the v8 <c>nitro-board</c> value: SQLite cannot ALTER a CHECK
    /// constraint in place, so the rebuild recreates the table under a
    /// fresh name, copies every row across, then swaps it in for the live
    /// one).
    /// </summary>
    private const string SessionPingGatesColumns =
        """
            harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot', 'nitro-board')),
            session_id TEXT NOT NULL,
            host TEXT NOT NULL,
            pid INTEGER NOT NULL CHECK (pid > 0),
            proc_start TEXT NOT NULL,
            attempt_id TEXT NOT NULL,
            acquired_at TEXT NOT NULL,
            expires_at TEXT NOT NULL,
            PRIMARY KEY (harness, session_id, host, pid, proc_start)
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
    /// instead of the live table name. <see cref="AgentDatabase"/> uses this
    /// to build a replacement table carrying the current CHECK constraint,
    /// copy every row from the live table into it, then swap it in under the
    /// live name, the standard SQLite rebuild for a CHECK constraint change
    /// no in-place ALTER can express.
    /// </summary>
    public static string CreateSessionPingGatesTable(string tableName) =>
        $"""
        CREATE TABLE "{tableName}" (
        {SessionPingGatesColumns}
        );
        """;
}
