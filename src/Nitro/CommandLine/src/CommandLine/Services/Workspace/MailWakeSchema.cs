namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Defines the durable wake outbox, claimed batches, target outcomes, and daemon
/// leadership leases. Each Nitro instance and actor can have at most one active batch.
/// </summary>
internal static class MailWakeSchema
{
    /// <summary>
    /// The columns and constraints of the wake target table.
    /// </summary>
    private const string MailWakeTargetsColumns =
        """
            batch_id TEXT NOT NULL REFERENCES mail_wake_batches (batch_id) ON DELETE CASCADE,
            harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot', 'opencode', 'nitro-board')),
            session_id TEXT NOT NULL,
            host TEXT NOT NULL,
            status TEXT NOT NULL DEFAULT 'pending'
                CHECK (status IN ('pending', 'delivered', 'satisfied', 'delegated', 'skipped', 'failed')),
            offered_generation INTEGER NULL CHECK (offered_generation IS NULL OR offered_generation >= 0),
            accepted_generation INTEGER NULL CHECK (accepted_generation IS NULL OR accepted_generation >= 0),
            last_error TEXT NULL CHECK (last_error IS NULL OR length(last_error) <= 200),
            updated_at TEXT NOT NULL,
            PRIMARY KEY (batch_id, harness, session_id, host)
        """;

    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS mail_wake_outbox (
            nitro_instance_id TEXT NOT NULL,
            actor TEXT NOT NULL REFERENCES agents (name),
            requested_generation INTEGER NOT NULL DEFAULT 0 CHECK (requested_generation >= 0),
            settled_generation INTEGER NOT NULL DEFAULT 0
                CHECK (settled_generation >= 0 AND settled_generation <= requested_generation),
            due_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            PRIMARY KEY (nitro_instance_id, actor)
        );

        CREATE INDEX IF NOT EXISTS idx_mail_wake_outbox_due
            ON mail_wake_outbox (due_at)
            WHERE settled_generation < requested_generation;

        CREATE TABLE IF NOT EXISTS mail_wake_batches (
            batch_id TEXT PRIMARY KEY,
            nitro_instance_id TEXT NOT NULL,
            actor TEXT NOT NULL,
            claimed_generation INTEGER NOT NULL CHECK (claimed_generation >= 0),
            owner_id TEXT NOT NULL,
            attempt_id TEXT NOT NULL,
            status TEXT NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'completed', 'released')),
            claimed_at TEXT NOT NULL,
            expires_at TEXT NOT NULL,
            completed_at TEXT NULL,
            last_error TEXT NULL CHECK (last_error IS NULL OR length(last_error) <= 200),
            FOREIGN KEY (nitro_instance_id, actor) REFERENCES mail_wake_outbox (nitro_instance_id, actor)
        );

        CREATE UNIQUE INDEX IF NOT EXISTS idx_mail_wake_batches_one_active_per_actor
            ON mail_wake_batches (nitro_instance_id, actor)
            WHERE status = 'active';

        CREATE INDEX IF NOT EXISTS idx_mail_wake_batches_expires
            ON mail_wake_batches (expires_at)
            WHERE status = 'active';

        CREATE TABLE IF NOT EXISTS mail_wake_targets (
        """
        + MailWakeTargetsColumns
        + """

        );

        CREATE TABLE IF NOT EXISTS mail_wake_daemons (
            nitro_instance_id TEXT PRIMARY KEY,
            owner_id TEXT NOT NULL,
            epoch INTEGER NOT NULL CHECK (epoch >= 1),
            leased_at TEXT NOT NULL,
            expires_at TEXT NOT NULL,
            last_error TEXT NULL CHECK (last_error IS NULL OR length(last_error) <= 200)
        );
        """;

    /// <summary>
    /// Returns SQL to create wake targets under <paramref name="tableName"/>.
    /// </summary>
    public static string CreateMailWakeTargetsTable(string tableName) =>
        $"""
        CREATE TABLE "{tableName}" (
        {MailWakeTargetsColumns}
        );
        """;
}
