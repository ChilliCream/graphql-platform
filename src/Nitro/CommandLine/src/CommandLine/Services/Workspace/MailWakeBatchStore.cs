using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class MailWakeBatchStore(IFileSystem fileSystem, AgentDatabase database) : IMailWakeBatchStore
{
    public async Task<MailWakeBatchClaim?> TryClaimAsync(
        string actor,
        string ownerId,
        string attemptId,
        IReadOnlyList<string> targets,
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
                WHERE actor = @actor
                """,
                new { actor },
                transaction: transaction);

        if (outbox is null
            || outbox.SettledGeneration >= outbox.RequestedGeneration
            || DateTimeOffset.Parse(outbox.DueAt, System.Globalization.CultureInfo.InvariantCulture) > now)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        // Expired active batches are released before another batch is claimed.
        await connection.ExecuteAsync(
            """
                UPDATE mail_wake_batches SET status = 'released', last_error = 'lease expired'
                WHERE actor = @actor AND status = 'active' AND expires_at <= @now
                """,
                new { actor, now },
                transaction: transaction);

        var activeBatchCount = await connection.ExecuteScalarAsync<long>(
            """
                SELECT COUNT(*) FROM mail_wake_batches
                WHERE actor = @actor AND status = 'active'
                """,
                new { actor },
                transaction: transaction);

        if (activeBatchCount > 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var isLeaseHolder = await connection.ExecuteScalarAsync<long>(
            """
                SELECT EXISTS (
                    SELECT 1 FROM mail_wake_daemons
                    WHERE id = 1 AND owner_token = @ownerId AND expires_at > @now
                )
                """,
                new { ownerId, now },
                transaction: transaction);

        if (isLeaseHolder == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        var batchId = Guid.NewGuid().ToString("N");
        var expiresAt = now + leaseDuration;

        await connection.ExecuteAsync(
            """
                INSERT INTO mail_wake_batches (
                    batch_id, actor, claimed_generation, owner_id, attempt_id,
                    status, claimed_at, expires_at
                ) VALUES (
                    @batchId, @actor, @claimedGeneration, @ownerId, @attemptId,
                    'active', @now, @expiresAt
                )
                """,
                new
                {
                    batchId,
                    actor,
                    claimedGeneration = outbox.RequestedGeneration,
                    ownerId,
                    attemptId,
                    now,
                    expiresAt
                },
                transaction: transaction);

        foreach (var target in targets)
        {
            await connection.ExecuteAsync(
                """
                    INSERT INTO mail_wake_targets (batch_id, agent, status, updated_at)
                    VALUES (@batchId, @target, 'pending', @now)
                    """,
                    new { batchId, target, now },
                    transaction: transaction);
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
                  AND EXISTS (
                      SELECT 1 FROM mail_wake_daemons
                      WHERE id = 1 AND owner_token = @ownerId AND expires_at > @now
                  )
                RETURNING batch_id
                """,
                new { batchId, ownerId, attemptId, now, expiresAt = now + leaseDuration });

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
                  AND EXISTS (
                      SELECT 1 FROM mail_wake_daemons
                      WHERE id = 1 AND owner_token = @ownerId AND expires_at > @now
                  )
                RETURNING actor AS Actor, claimed_generation AS ClaimedGeneration
                """,
                new { batchId, ownerId, attemptId, now },
                transaction: transaction);

        if (completed is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        // Completion settles through the claimed generation without lowering existing settlement.
        await connection.ExecuteAsync(
            """
                UPDATE mail_wake_outbox
                SET settled_generation = MAX(settled_generation, @claimedGeneration), updated_at = @now
                WHERE actor = @actor
                """,
                new
                {
                    claimedGeneration = completed.ClaimedGeneration,
                    now,
                    actor = completed.Actor
                },
                transaction: transaction);

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
                WHERE batch_id = @batchId AND owner_id = @ownerId AND attempt_id = @attemptId
                  AND status = 'active' AND expires_at > @now
                  AND EXISTS (
                      SELECT 1 FROM mail_wake_daemons
                      WHERE id = 1 AND owner_token = @ownerId AND expires_at > @now
                  )
                RETURNING actor AS Actor
                """,
                new { batchId, ownerId, attemptId, now, lastError },
                transaction: transaction);

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
                    WHERE actor = @actor
                    """,
                    new { retryAt = retryAtValue, now, actor = released.Actor },
                    transaction: transaction);
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryRecordTargetOutcomeAsync(
        string batchId,
        string target,
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
                WHERE batch_id = @batchId AND agent = @target
                  AND EXISTS (
                      SELECT 1 FROM mail_wake_batches
                      WHERE batch_id = @batchId AND owner_id = @ownerId AND attempt_id = @attemptId
                        AND status = 'active' AND expires_at > @now
                  )
                  AND EXISTS (
                      SELECT 1 FROM mail_wake_daemons
                      WHERE id = 1 AND owner_token = @ownerId AND expires_at > @now
                  )
                RETURNING batch_id
                """,
                new
                {
                    batchId,
                    target,
                    status,
                    offeredGeneration,
                    acceptedGeneration,
                    lastError,
                    now,
                    ownerId,
                    attemptId
                });

        return updatedBatchId is not null;
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }

    internal sealed class OutboxDueRow
    {
        public required long RequestedGeneration { get; init; }
        public required long SettledGeneration { get; init; }
        public required string DueAt { get; init; }
    }

    internal sealed class CompletedBatchRow
    {
        public required string Actor { get; init; }
        public required long ClaimedGeneration { get; init; }
    }

    internal sealed class ReleasedBatchRow
    {
        public required string Actor { get; init; }
    }
}
