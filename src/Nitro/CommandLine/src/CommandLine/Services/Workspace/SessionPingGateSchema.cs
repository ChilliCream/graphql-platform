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
/// database is non-destructive.
/// </summary>
internal static class SessionPingGateSchema
{
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS session_ping_gates (
            harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot')),
            session_id TEXT NOT NULL,
            host TEXT NOT NULL,
            pid INTEGER NOT NULL CHECK (pid > 0),
            proc_start TEXT NOT NULL,
            attempt_id TEXT NOT NULL,
            acquired_at TEXT NOT NULL,
            expires_at TEXT NOT NULL,
            PRIMARY KEY (harness, session_id, host, pid, proc_start)
        );

        CREATE INDEX IF NOT EXISTS idx_session_ping_gates_expires ON session_ping_gates (expires_at);
        """;
}
