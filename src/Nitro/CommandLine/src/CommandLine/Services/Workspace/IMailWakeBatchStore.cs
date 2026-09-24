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
    /// Claims due, unsettled work for the instance and actor, replacing any expired
    /// batch and recording the current requested generation and supplied targets.
    /// Returns null when no work is due or an unexpired active batch exists.
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
    /// Sets the active batch's expiry to <paramref name="now"/> plus <paramref name="leaseDuration"/>.
    /// Returns false when the owner or attempt differs, or the batch is inactive or expired.
    /// </summary>
    Task<bool> TryRenewAsync(
        string batchId,
        string ownerId,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Completes the matching active batch and advances settlement through its claimed
    /// generation, preserving any higher settlement. Returns false when the owner or
    /// attempt differs, or the batch is inactive or expired.
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
    /// Records the status, generations, and error for the matching target; null values
    /// clear the corresponding fields. Returns false when the target is missing or the
    /// batch is inactive, expired, or owned by a different owner or attempt.
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
