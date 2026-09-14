using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Delegates every <see cref="IMailWakeBatchStore"/> member to
/// <paramref name="inner"/> except <see cref="TryRenewAsync"/>, which always
/// throws - simulating the renewal store call itself failing outright,
/// distinct from it merely returning false.
/// </summary>
internal sealed class ThrowingRenewMailWakeBatchStore(IMailWakeBatchStore inner) : IMailWakeBatchStore
{
    public Task<MailWakeBatchClaim?> TryClaimAsync(
        string nitroInstanceId, string actor, string ownerId, string attemptId,
        IReadOnlyList<AgentSessionGeneration> targets, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => inner.TryClaimAsync(nitroInstanceId, actor, ownerId, attemptId, targets, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated TryRenewAsync failure.");

    public Task<bool> TryCompleteAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryCompleteAsync(batchId, ownerId, attemptId, now, cancellationToken);

    public Task<bool> TryReleaseAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, DateTimeOffset? retryAt,
        string? lastError, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(batchId, ownerId, attemptId, now, retryAt, lastError, cancellationToken);

    public Task<bool> TryRecordTargetOutcomeAsync(
        string batchId, AgentSessionGeneration target, string ownerId, string attemptId, string status,
        long? offeredGeneration, long? acceptedGeneration, string? lastError, DateTimeOffset now,
        CancellationToken cancellationToken)
        => inner.TryRecordTargetOutcomeAsync(
            batchId, target, ownerId, attemptId, status, offeredGeneration, acceptedGeneration, lastError, now,
            cancellationToken);
}
