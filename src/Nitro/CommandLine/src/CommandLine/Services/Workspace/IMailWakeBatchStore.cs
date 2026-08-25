namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Atomic conditional primitives over <c>mail_wake_batches</c>,
/// <c>mail_wake_targets</c>, and the claim/settle side of
/// <c>mail_wake_outbox</c>. Every mutation is owner/attempt/expiry fenced:
/// it only applies when the caller still holds the exact claim it is acting
/// on, so a renewal or completion lost to a fresher claimant becomes a
/// silent no-op rather than corrupting state a new owner has since taken
/// over. Does not decide which actor to claim next, which sessions belong in
/// a batch, or how to react to a target's outcome; those policies belong to
/// the caller (the direct-first wake dispatcher).
/// </summary>
internal interface IMailWakeBatchStore
{
    /// <summary>
    /// Atomically claims the next batch of outstanding wake work for
    /// (<paramref name="nitroInstanceId"/>, <paramref name="actor"/>) and
    /// materializes <paramref name="targets"/> as its frozen
    /// <c>mail_wake_targets</c> rows. Returns null when there is no
    /// outstanding generation (<c>settled_generation</c> already equals
    /// <c>requested_generation</c>), the outbox row's <c>due_at</c> has not
    /// yet arrived, or an active batch already exists for this actor (at
    /// most one active batch per actor at a time). The returned claim's
    /// <see cref="MailWakeBatchClaim.ClaimedGeneration"/> is fixed for this
    /// batch's whole lifetime, snapshotted from <c>requested_generation</c>
    /// at claim time.
    /// </summary>
    Task<MailWakeBatchClaim?> TryClaimAsync(
        string nitroInstanceId,
        string actor,
        string ownerId,
        string attemptId,
        IReadOnlyList<AgentSessionGeneration> targets,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Extends an active batch's expiry. Returns false, changing nothing,
    /// when the batch is not active, does not match
    /// <paramref name="ownerId"/> and <paramref name="attemptId"/>, or has
    /// already expired (a lost lease can never be renewed back to life; the
    /// caller must re-claim).
    /// </summary>
    Task<bool> TryRenewAsync(
        string batchId,
        string ownerId,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an active batch completed and settles the outbox row's
    /// <c>settled_generation</c> up to (never past) this batch's own
    /// <see cref="MailWakeBatchRecord.ClaimedGeneration"/>: a wake requested
    /// after this batch was claimed is never settled by this completion,
    /// regardless of how far <c>requested_generation</c> has since advanced.
    /// Returns false, changing nothing, under the same fencing as
    /// <see cref="TryRenewAsync"/>.
    /// </summary>
    Task<bool> TryCompleteAsync(
        string batchId,
        string ownerId,
        string attemptId,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an active batch released without settling any generation,
    /// freeing the actor for a future claim. When <paramref name="retryAt"/>
    /// is given, sets the outbox row's <c>due_at</c> to it, scheduling the
    /// next claim attempt; arbitrating that against a due time a concurrent
    /// send may set in the meantime is the enqueueing caller's policy, not
    /// this primitive's. Returns false, changing nothing, under the same
    /// fencing as <see cref="TryRenewAsync"/>.
    /// </summary>
    Task<bool> TryReleaseAsync(
        string batchId,
        string ownerId,
        string attemptId,
        DateTimeOffset now,
        DateTimeOffset? retryAt,
        string? lastError,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records one target's outcome: its lattice <paramref name="status"/>
    /// and its target-qualified <paramref name="offeredGeneration"/> and
    /// <paramref name="acceptedGeneration"/>, scoped to this one target row,
    /// never implied for the rest of the batch. Fenced through the owning
    /// batch: a no-op returning false when the batch is not active or does
    /// not match <paramref name="ownerId"/> and <paramref name="attemptId"/>.
    /// </summary>
    Task<bool> TryRecordTargetOutcomeAsync(
        string batchId,
        AgentSessionGeneration target,
        string ownerId,
        string attemptId,
        string status,
        long? offeredGeneration,
        long? acceptedGeneration,
        string? lastError,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
