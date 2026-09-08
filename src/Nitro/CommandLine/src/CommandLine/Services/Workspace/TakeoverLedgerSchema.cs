namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal static class TakeoverLedgerSchema
{
    /// <summary>
    /// The complete schema for recorded actor takeovers. Statements are idempotent so applying them to an existing database is non-destructive.
    /// </summary>
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS agent_takeovers (
            id TEXT PRIMARY KEY,
            from_actor TEXT NOT NULL,
            to_actor TEXT NOT NULL,
            actor TEXT NOT NULL,
            created_at TEXT NOT NULL,
            forced INTEGER NOT NULL,
            role TEXT NULL,
            reason TEXT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_agent_takeovers_from_actor
            ON agent_takeovers (from_actor);
        CREATE INDEX IF NOT EXISTS idx_agent_takeovers_to_actor
            ON agent_takeovers (to_actor);

        CREATE TABLE IF NOT EXISTS agent_takeover_items (
            takeover_id TEXT NOT NULL REFERENCES agent_takeovers (id) ON DELETE CASCADE,
            kind TEXT NOT NULL CHECK (kind IN ('message_sender', 'message_recipient', 'task')),
            item_id TEXT NOT NULL,
            PRIMARY KEY (takeover_id, kind, item_id)
        );

        CREATE INDEX IF NOT EXISTS idx_agent_takeover_items_kind_item_id
            ON agent_takeover_items (kind, item_id);
        """;
}
