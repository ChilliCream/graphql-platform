namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Schema v7: the durable actor-wake queue and its claimed dispatch state,
/// added alongside <see cref="SessionPingGateSchema"/>. Four tables.
/// <c>mail_wake_outbox</c> is the durable per-(Nitro instance, actor) queue
/// head: <c>requested_generation</c> counts every distinct wake intent ever
/// enqueued for that actor on this instance, <c>settled_generation</c> is
/// the highest generation a completed batch has actually settled (never
/// ahead of a generation whose batch has not yet completed), and
/// <c>due_at</c> is the earliest time outstanding work should next be
/// attempted. <c>mail_wake_batches</c> is one immutable claim against an
/// outbox row: <c>claimed_generation</c> snapshots
/// <c>requested_generation</c> at claim time so completing this batch can
/// never settle a generation requested after the claim, and
/// <c>owner_id</c>/<c>attempt_id</c>/<c>expires_at</c> fence every renewal
/// and completion against a stale or superseded claimant. At most one
/// <c>active</c> batch exists per actor at a time, enforced by
/// <c>idx_mail_wake_batches_one_active_per_actor</c>. <c>mail_wake_targets</c>
/// is one row per full session generation a batch dispatched to when
/// claimed, cascading only with its owning batch, never with
/// <c>agent_sessions</c>: a target row is a durable record of what a batch
/// offered or was accepted for, and must survive the session that received
/// it ending or being reaped. <c>offered_generation</c> and
/// <c>accepted_generation</c> record target-qualified acceptance (a
/// dashboard or peer accepting responsibility for that one target, not the
/// whole batch). <c>mail_wake_daemons</c> is one persistent leader row per
/// Nitro instance: <c>epoch</c> increments every time a new owner steals an
/// expired lease, so a stale owner's write is rejected by its own
/// no-longer-current epoch rather than merely by wall-clock expiry. Every
/// <c>last_error</c> column is bounded the same way
/// <c>agent_sessions.last_ping_detail</c> is, so a stored diagnostic can
/// never grow unbounded. Statements are idempotent so applying them to an
/// existing database is non-destructive.
/// </summary>
internal static class MailWakeSchema
{
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
            batch_id TEXT NOT NULL REFERENCES mail_wake_batches (batch_id) ON DELETE CASCADE,
            harness TEXT NOT NULL CHECK (harness IN ('claude-code', 'codex', 'copilot')),
            session_id TEXT NOT NULL,
            host TEXT NOT NULL,
            pid INTEGER NOT NULL CHECK (pid > 0),
            proc_start TEXT NOT NULL,
            status TEXT NOT NULL DEFAULT 'pending'
                CHECK (status IN ('pending', 'delivered', 'satisfied', 'delegated', 'skipped', 'failed')),
            offered_generation INTEGER NULL CHECK (offered_generation IS NULL OR offered_generation >= 0),
            accepted_generation INTEGER NULL CHECK (accepted_generation IS NULL OR accepted_generation >= 0),
            last_error TEXT NULL CHECK (last_error IS NULL OR length(last_error) <= 200),
            updated_at TEXT NOT NULL,
            PRIMARY KEY (batch_id, harness, session_id, host, pid, proc_start)
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
}
