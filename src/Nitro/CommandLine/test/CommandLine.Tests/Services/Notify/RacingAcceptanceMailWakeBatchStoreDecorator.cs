using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Attempts to record <see cref="MailWakeTargetStatus.Delivered"/> before forwarding
/// the first outcome write for <paramref name="racedTarget"/>, using the caller's owner and attempt.
/// Delegates all other <see cref="IMailWakeBatchStore"/> calls to <paramref name="inner"/>.
/// </summary>
internal sealed class RacingAcceptanceMailWakeBatchStoreDecorator(
    IMailWakeBatchStore inner, AgentSessionGeneration racedTarget, long acceptedGeneration) : IMailWakeBatchStore
{
    private bool _raced;

    public Task<MailWakeBatchClaim?> TryClaimAsync(
        string nitroInstanceId, string actor, string ownerId, string attemptId,
        IReadOnlyList<AgentSessionGeneration> targets, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => inner.TryClaimAsync(nitroInstanceId, actor, ownerId, attemptId, targets, now, leaseDuration, cancellationToken);

    public Task<bool> TryRenewAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => inner.TryRenewAsync(batchId, ownerId, attemptId, now, leaseDuration, cancellationToken);

    public Task<bool> TryCompleteAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, CancellationToken cancellationToken)
        => inner.TryCompleteAsync(batchId, ownerId, attemptId, now, cancellationToken);

    public Task<bool> TryReleaseAsync(
        string batchId, string ownerId, string attemptId, DateTimeOffset now, DateTimeOffset? retryAt,
        string? lastError, CancellationToken cancellationToken)
        => inner.TryReleaseAsync(batchId, ownerId, attemptId, now, retryAt, lastError, cancellationToken);

    public async Task<bool> TryRecordTargetOutcomeAsync(
        string batchId, AgentSessionGeneration target, string ownerId, string attemptId, string status,
        long? offeredGeneration, long? acceptedGeneration_, string? lastError, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!_raced && target == racedTarget)
        {
            _raced = true;
            await inner.TryRecordTargetOutcomeAsync(
                batchId, target, ownerId, attemptId, MailWakeTargetStatus.Delivered,
                offeredGeneration: null, acceptedGeneration, lastError: null, now, cancellationToken);
        }

        return await inner.TryRecordTargetOutcomeAsync(
            batchId, target, ownerId, attemptId, status, offeredGeneration, acceptedGeneration_, lastError, now,
            cancellationToken);
    }
}
