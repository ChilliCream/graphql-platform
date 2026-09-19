namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The durable actor-wake queue and its claimed dispatch state. Four
/// tables. <c>mail_wake_outbox</c> is the durable per-(Nitro instance,
/// actor) queue head: <c>requested_generation</c> counts every distinct
/// wake intent ever enqueued for that actor on this instance,
/// <c>settled_generation</c> is the highest generation a completed batch
/// has actually settled, and <c>due_at</c> is the earliest time outstanding
/// work should next be attempted. <c>mail_wake_batches</c> is one immutable
/// claim against an outbox row, fenced by <c>owner_id</c>/<c>attempt_id</c>/
/// <c>expires_at</c> against a stale or superseded claimant; at most one
/// <c>active</c> batch exists per actor at a time, enforced by
/// <c>idx_mail_wake_batches_one_active_per_actor</c>. <c>mail_wake_targets</c>
/// is one row per full session generation a batch dispatched to when
/// claimed, cascading only with its owning batch, never with
/// <c>agent_sessions</c>; <c>offered_generation</c> and
/// <c>accepted_generation</c> record target-qualified acceptance.
/// <c>mail_wake_daemons</c> is one persistent leader row per Nitro instance,
/// with <c>epoch</c> incrementing every time a new owner steals an expired
/// lease. Every <c>last_error</c> column is bounded the same way
/// <c>agent_sessions.last_ping_detail</c> is. Statements are idempotent so
/// applying them to an existing database is non-destructive.
/// </summary>
internal static class MailWakeSchema
{
    /// <summary>
    /// The <c>mail_wake_targets</c> column and constraint list, shared
    /// between <see cref="Create"/> (applied under the live table name) and
    /// <see cref="CreateMailWakeTargetsTable"/> (applied under a temporary
    /// name to rebuild the table).
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
    /// The same <c>mail_wake_targets</c> column and constraint list as
    /// <see cref="Create"/>, applied under <paramref name="tableName"/>
    /// instead of the live table name.
    /// </summary>
    public static string CreateMailWakeTargetsTable(string tableName) =>
        $"""
        CREATE TABLE "{tableName}" (
        {MailWakeTargetsColumns}
        );
        """;
}
