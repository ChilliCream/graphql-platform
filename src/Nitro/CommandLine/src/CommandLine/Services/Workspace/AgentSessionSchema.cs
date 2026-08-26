namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Schema v5: agent presence and mail delivery/ping bookkeeping, added
/// alongside the identity table in <see cref="AgentRegistrySchema"/>. Three
/// tables: <c>agent_sessions</c> (the canonical active-membership table, one
/// row per live harness session, claimed or not, keyed by
/// (harness, session_id)); <c>session_deliveries</c> (the at-most-once-per-
/// channel notification ledger, cascading with its owning session); and
/// <c>ping_leases</c> (the fixed four-slot concurrency cap on outstanding
/// ping children). Statements are idempotent so applying them to an
/// existing database is non-destructive. <c>last_ping_result</c> carries
/// <c>unsupported</c> for an endpoint kind the notifier has no transport
/// for (<c>claude-peer</c>, currently): a distinct diagnostic from
/// <c>endpoint_kind = 'none'</c>, which means the session simply has no
/// endpoint to attempt at all. <c>agent_sessions</c> also carries the
/// mutable participant <c>role</c> and the exact <c>harness_version</c>.
/// v8 adds <c>nitro-board</c> to the <c>harness</c> CHECK constraint (a
/// running board process, bound to the durable human mail actor as an
/// operator participant instead of a coding-harness hook) and
/// <c>db-watch</c> to the <c>endpoint_kind</c> CHECK constraint (the shared
/// workspace database file itself as the delivery endpoint, with no
/// routable address and no transport ever fired against it). v10 drops the
/// <c>pid</c>, <c>proc_start</c>, <c>process_scope</c> and
/// <c>proc_start_legacy</c> columns: a hook event names its own session, so
/// (harness, session_id, host) identifies a row exactly and no process
/// identity is recorded or compared.
/// </summary>
internal static class AgentSessionSchema
{
    /// <summary>
    /// The <c>agent_sessions</c> column and constraint list, shared between
    /// <see cref="Create"/> (which applies it under the live table name) and
    /// <see cref="CreateAgentSessionsTable"/> (which <see cref="AgentDatabase"/>
    /// applies under a temporary name to rebuild the table for a database
    /// whose <c>last_ping_result</c> CHECK constraint predates
    /// <c>unsupported</c>: SQLite cannot ALTER a CHECK constraint in place,
    /// so the rebuild recreates the table under a fresh name, copies every
    /// row across, then swaps it in for the live one).
    /// </summary>
    private const string AgentSessionsColumns =
        """
            harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot', 'nitro-board')),
            session_id TEXT NOT NULL,
            agent_name TEXT NULL REFERENCES agents (name),
            binding_kind TEXT NOT NULL DEFAULT 'none' CHECK (binding_kind IN ('none', 'env', 'explicit')),
            host TEXT NOT NULL,
            cwd TEXT NOT NULL,
            workspace_path TEXT NOT NULL,
            endpoint_kind TEXT NOT NULL CHECK (endpoint_kind IN ('claude-peer', 'codex-thread', 'copilot-extension', 'db-watch', 'none')),
            endpoint_addr TEXT NOT NULL,
            started_at TEXT NOT NULL,
            last_beat_at TEXT NOT NULL,
            block_budget_used INTEGER NOT NULL DEFAULT 0 CHECK (block_budget_used >= 0),
            last_ping_at TEXT NULL,
            last_ping_attempt TEXT NULL,
            last_ping_result TEXT NULL CHECK (last_ping_result IN ('ok', 'spawn-failed', 'endpoint-gone', 'timeout', 'capacity-dropped', 'error', 'unsupported') OR last_ping_result IS NULL),
            last_ping_detail TEXT NULL CHECK (last_ping_detail IS NULL OR length(last_ping_detail) <= 200),
            role TEXT NOT NULL DEFAULT '',
            harness_version TEXT NOT NULL DEFAULT '',
            -- Table-level CHECK constraints must follow every column
            -- definition (SQLite rejects one interleaved between columns),
            -- so both cross-column checks live here instead of next to the
            -- columns they compare.
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
    /// The same <c>agent_sessions</c> column and constraint list as
    /// <see cref="Create"/>, applied under <paramref name="tableName"/>
    /// instead of the live table name. <see cref="AgentDatabase"/> uses this
    /// to build a replacement table carrying the current CHECK constraint,
    /// copy every row from the live table into it, then swap it in under the
    /// live name, the standard SQLite rebuild for a CHECK constraint change
    /// no in-place ALTER can express.
    /// </summary>
    public static string CreateAgentSessionsTable(string tableName) =>
        $"""
        CREATE TABLE "{tableName}" (
        {AgentSessionsColumns}
        );
        """;
}
