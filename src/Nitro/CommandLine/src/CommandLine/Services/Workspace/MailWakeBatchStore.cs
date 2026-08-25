using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class MailWakeBatchStore(IFileSystem fileSystem, AgentDatabase database) : IMailWakeBatchStore
{
    public async Task<MailWakeBatchClaim?> TryClaimAsync(
        string nitroInstanceId,
        string actor,
        string ownerId,
        string attemptId,
        IReadOnlyList<AgentSessionGeneration> targets,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var outbox = await connection.QueryFirstOrDefaultAsync<OutboxDueRow>(
            """
            SELECT requested_generation AS RequestedGeneration, settled_generation AS SettledGeneration,
                   due_at AS DueAt
            FROM mail_wake_outbox
            WHERE nitro_instance_id = @nitroInstanceId AND actor = @actor
            """,
            new { nitroInstanceId, actor, cancellationToken },
            transaction);

        if (outbox is null
            || outbox.SettledGeneration >= outbox.RequestedGeneration
            || DateTimeOffset.Parse(outbox.DueAt, System.Globalization.CultureInfo.InvariantCulture) > now)
        {
            // Nothing outstanding, or not due yet: commit to release the
            // read lock promptly rather than leave it for disposal.
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        // Reclaim an expired active batch before checking for a live one:
        // a crashed owner's lease never gets renewed, so without this an
        // expired row would wedge the actor's queue forever. Mirrors the
        // steal-if-expired shape in MailWakeDaemonLeaderStore.TryAcquireAsync.
        await connection.ExecuteAsync(
            """
            UPDATE mail_wake_batches SET status = 'released', last_error = 'lease expired'
            WHERE nitro_instance_id = @nitroInstanceId AND actor = @actor AND status = 'active'
              AND expires_at <= @now
            """,
            new { nitroInstanceId, actor, now, cancellationToken },
            transaction);

        // Belt-and-braces alongside idx_mail_wake_batches_one_active_per_actor:
        // checked explicitly so a losing caller gets a clean null instead of
        // a thrown constraint-violation exception from the insert below.
        var activeBatchCount = await connection.ExecuteScalarAsync<long>(
            """
            SELECT COUNT(*) FROM mail_wake_batches
            WHERE nitro_instance_id = @nitroInstanceId AND actor = @actor AND status = 'active'
            """,
            new { nitroInstanceId, actor, cancellationToken },
            transaction);

        if (activeBatchCount > 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var batchId = Guid.NewGuid().ToString("N");
        var expiresAt = now + leaseDuration;

        await connection.ExecuteAsync(
            """
            INSERT INTO mail_wake_batches (
                batch_id, nitro_instance_id, actor, claimed_generation, owner_id, attempt_id,
                status, claimed_at, expires_at
            ) VALUES (
                @batchId, @nitroInstanceId, @actor, @claimedGeneration, @ownerId, @attemptId,
                'active', @now, @expiresAt
            )
            """,
            new
            {
                batchId,
                nitroInstanceId,
                actor,
                claimedGeneration = outbox.RequestedGeneration,
                ownerId,
                attemptId,
                now,
                expiresAt,
                cancellationToken
            },
            transaction);

        foreach (var target in targets)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO mail_wake_targets (batch_id, harness, session_id, host, pid, proc_start, status, updated_at)
                VALUES (@batchId, @harness, @sessionId, @host, @pid, @procStart, 'pending', @now)
                """,
                new
                {
                    batchId,
                    harness = target.Harness,
                    sessionId = target.SessionId,
                    host = target.Host,
                    pid = target.Pid,
                    procStart = target.ProcStart,
                    now,
                    cancellationToken
                },
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);

        return new MailWakeBatchClaim(batchId, outbox.RequestedGeneration, targets);
    }

    public async Task<bool> TryRenewAsync(
        string batchId,
        string ownerId,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var renewedBatchId = await connection.QueryFirstOrDefaultAsync<string>(
            """
            UPDATE mail_wake_batches SET expires_at = @expiresAt
            WHERE batch_id = @batchId AND owner_id = @ownerId AND attempt_id = @attemptId
              AND status = 'active' AND expires_at > @now
            RETURNING batch_id
            """,
            new { batchId, ownerId, attemptId, now, expiresAt = now + leaseDuration, cancellationToken });

        return renewedBatchId is not null;
    }

    public async Task<bool> TryCompleteAsync(
        string batchId,
        string ownerId,
        string attemptId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var completed = await connection.QueryFirstOrDefaultAsync<CompletedBatchRow>(
            """
            UPDATE mail_wake_batches SET status = 'completed', completed_at = @now
            WHERE batch_id = @batchId AND owner_id = @ownerId AND attempt_id = @attemptId
              AND status = 'active' AND expires_at > @now
            RETURNING nitro_instance_id AS NitroInstanceId, actor AS Actor, claimed_generation AS ClaimedGeneration
            """,
            new { batchId, ownerId, attemptId, now, cancellationToken },
            transaction);

        if (completed is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        // MAX(...) is what makes this "never settle G+1": ClaimedGeneration
        // is fixed at claim time, so even if requested_generation has since
        // advanced past it, settled_generation only ever catches up to the
        // generation this exact batch claimed.
        await connection.ExecuteAsync(
            """
            UPDATE mail_wake_outbox
            SET settled_generation = MAX(settled_generation, @claimedGeneration), updated_at = @now
            WHERE nitro_instance_id = @nitroInstanceId AND actor = @actor
            """,
            new
            {
                claimedGeneration = completed.ClaimedGeneration,
                now,
                nitroInstanceId = completed.NitroInstanceId,
                actor = completed.Actor,
                cancellationToken
            },
            transaction);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryReleaseAsync(
        string batchId,
        string ownerId,
        string attemptId,
        DateTimeOffset now,
        DateTimeOffset? retryAt,
        string? lastError,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var released = await connection.QueryFirstOrDefaultAsync<ReleasedBatchRow>(
            """
            UPDATE mail_wake_batches SET status = 'released', last_error = @lastError
            WHERE batch_id = @batchId AND owner_id = @ownerId AND attempt_id = @attemptId AND status = 'active'
            RETURNING nitro_instance_id AS NitroInstanceId, actor AS Actor
            """,
            new { batchId, ownerId, attemptId, lastError, cancellationToken },
            transaction);

        if (released is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        if (retryAt is { } retryAtValue)
        {
            await connection.ExecuteAsync(
                """
                UPDATE mail_wake_outbox SET due_at = @retryAt, updated_at = @now
                WHERE nitro_instance_id = @nitroInstanceId AND actor = @actor
                """,
                new
                {
                    retryAt = retryAtValue,
                    now,
                    nitroInstanceId = released.NitroInstanceId,
                    actor = released.Actor,
                    cancellationToken
                },
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryRecordTargetOutcomeAsync(
        string batchId,
        AgentSessionGeneration target,
        string ownerId,
        string attemptId,
        string status,
        long? offeredGeneration,
        long? acceptedGeneration,
        string? lastError,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var updatedBatchId = await connection.QueryFirstOrDefaultAsync<string>(
            """
            UPDATE mail_wake_targets SET
                status = @status,
                offered_generation = @offeredGeneration,
                accepted_generation = @acceptedGeneration,
                last_error = @lastError,
                updated_at = @now
            WHERE batch_id = @batchId AND harness = @harness AND session_id = @sessionId
              AND host = @host AND pid = @pid AND proc_start = @procStart
              AND EXISTS (
                  SELECT 1 FROM mail_wake_batches
                  WHERE batch_id = @batchId AND owner_id = @ownerId AND attempt_id = @attemptId
                    AND status = 'active' AND expires_at > @now
              )
            RETURNING batch_id
            """,
            new
            {
                batchId,
                harness = target.Harness,
                sessionId = target.SessionId,
                host = target.Host,
                pid = target.Pid,
                procStart = target.ProcStart,
                status,
                offeredGeneration,
                acceptedGeneration,
                lastError,
                now,
                ownerId,
                attemptId,
                cancellationToken
            });

        return updatedBatchId is not null;
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }

    // Internal, not private: Dapper.AOT's generated interceptors live
    // outside this class and cannot reference a private nested type.
    internal sealed class OutboxDueRow
    {
        public required long RequestedGeneration { get; init; }
        public required long SettledGeneration { get; init; }
        public required string DueAt { get; init; }
    }

    internal sealed class CompletedBatchRow
    {
        public required string NitroInstanceId { get; init; }
        public required string Actor { get; init; }
        public required long ClaimedGeneration { get; init; }
    }

    internal sealed class ReleasedBatchRow
    {
        public required string NitroInstanceId { get; init; }
        public required string Actor { get; init; }
    }
}
