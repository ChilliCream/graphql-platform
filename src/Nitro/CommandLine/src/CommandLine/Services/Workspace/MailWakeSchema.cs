namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Defines the durable wake outbox, claimed batches, target outcomes, and the single
/// daemon leadership lease. Each actor can have at most one active batch.
/// </summary>
internal static class MailWakeSchema
{
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS mail_wake_outbox (
            actor TEXT NOT NULL PRIMARY KEY REFERENCES agents (name),
            requested_generation INTEGER NOT NULL DEFAULT 0 CHECK (requested_generation >= 0),
            settled_generation INTEGER NOT NULL DEFAULT 0
                CHECK (settled_generation >= 0 AND settled_generation <= requested_generation),
            due_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_mail_wake_outbox_due
            ON mail_wake_outbox (due_at)
            WHERE settled_generation < requested_generation;

        CREATE TABLE IF NOT EXISTS mail_wake_batches (
            batch_id TEXT PRIMARY KEY,
            actor TEXT NOT NULL REFERENCES mail_wake_outbox (actor),
            claimed_generation INTEGER NOT NULL CHECK (claimed_generation >= 0),
            owner_id TEXT NOT NULL,
            attempt_id TEXT NOT NULL,
            status TEXT NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'completed', 'released')),
            claimed_at TEXT NOT NULL,
            expires_at TEXT NOT NULL,
            completed_at TEXT NULL,
            last_error TEXT NULL CHECK (last_error IS NULL OR length(last_error) <= 200)
        );

        CREATE UNIQUE INDEX IF NOT EXISTS idx_mail_wake_batches_one_active_per_actor
            ON mail_wake_batches (actor)
            WHERE status = 'active';

        CREATE INDEX IF NOT EXISTS idx_mail_wake_batches_expires
            ON mail_wake_batches (expires_at)
            WHERE status = 'active';

        CREATE TABLE IF NOT EXISTS mail_wake_targets (
            batch_id TEXT NOT NULL REFERENCES mail_wake_batches (batch_id) ON DELETE CASCADE,
            agent TEXT NOT NULL REFERENCES agents (name),
            status TEXT NOT NULL DEFAULT 'pending'
                CHECK (status IN ('pending', 'delivered', 'satisfied', 'delegated', 'skipped', 'failed')),
            offered_generation INTEGER NULL CHECK (offered_generation IS NULL OR offered_generation >= 0),
            accepted_generation INTEGER NULL CHECK (accepted_generation IS NULL OR accepted_generation >= 0),
            last_error TEXT NULL CHECK (last_error IS NULL OR length(last_error) <= 200),
            updated_at TEXT NOT NULL,
            PRIMARY KEY (batch_id, agent)
        );

        CREATE TABLE IF NOT EXISTS mail_wake_daemons (
            id INTEGER PRIMARY KEY CHECK (id = 1),
            owner_token TEXT NOT NULL,
            acquired_at TEXT NOT NULL,
            heartbeat_at TEXT NOT NULL,
            expires_at TEXT NOT NULL
        );
        """;
}
