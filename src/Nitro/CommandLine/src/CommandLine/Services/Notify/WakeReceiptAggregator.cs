namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Derives one actor-wake batch's aggregate status from its target rows'
/// individual <see cref="MailWakeTargetStatus"/> values. The zero
/// statuses (<see cref="MailWakeTargetStatus.Delivered"/>,
/// <see cref="MailWakeTargetStatus.Satisfied"/>,
/// <see cref="MailWakeTargetStatus.Delegated"/>,
/// <see cref="MailWakeTargetStatus.Skipped"/>) need nothing further from the
/// caller; <see cref="MailWakeTargetStatus.Pending"/>,
/// <see cref="MailWakeTargetStatus.Failed"/>, and <see cref="Partial"/> are
/// nonzero. A batch with no targets at all (no live session to address)
/// aggregates to <see cref="MailWakeTargetStatus.Failed"/>: nobody was, or
/// could be, notified.
/// </summary>
internal static class WakeReceiptAggregator
{
    /// <summary>
    /// At least one target failed and at least one sibling did not: a batch
    /// is never silently reported as clean when part of it genuinely
    /// failed.
    /// </summary>
    public const string Partial = "partial";

    public static bool IsZero(string status) => status switch
    {
        MailWakeTargetStatus.Delivered
            or MailWakeTargetStatus.Satisfied
            or MailWakeTargetStatus.Delegated
            or MailWakeTargetStatus.Skipped => true,
        _ => false
    };

    /// <summary>
    /// Combines every target's status into one batch-level verdict.
    /// <list type="bullet">
    /// <item>No targets at all: <see cref="MailWakeTargetStatus.Failed"/>
    /// (no-live-session).</item>
    /// <item>At least one <see cref="MailWakeTargetStatus.Failed"/> target
    /// alongside at least one non-failed sibling: <see cref="Partial"/>.</item>
    /// <item>Every target failed: <see cref="MailWakeTargetStatus.Failed"/>.</item>
    /// <item>No failures, but at least one target still
    /// <see cref="MailWakeTargetStatus.Pending"/>: <see cref="MailWakeTargetStatus.Pending"/>.</item>
    /// <item>Every target reached a zero status: the first target's own
    /// status, the representative value for an otherwise homogeneous batch.
    /// A batch whose zero-status targets disagree (some delivered, others
    /// satisfied) has no single representative defined by this bead; the
    /// first target's status is returned as a documented, deliberately
    /// conservative placeholder pending a future caller that needs to
    /// distinguish the mix.</item>
    /// </list>
    /// </summary>
    public static string Aggregate(IReadOnlyList<string> targetStatuses)
    {
        if (targetStatuses.Count == 0)
        {
            return MailWakeTargetStatus.Failed;
        }

        var failedCount = 0;
        var nonFailedCount = 0;
        var hasPending = false;

        foreach (var status in targetStatuses)
        {
            if (status == MailWakeTargetStatus.Failed)
            {
                failedCount++;
            }
            else
            {
                nonFailedCount++;
            }

            if (status == MailWakeTargetStatus.Pending)
            {
                hasPending = true;
            }
        }

        if (failedCount > 0 && nonFailedCount > 0)
        {
            return Partial;
        }

        if (failedCount > 0)
        {
            return MailWakeTargetStatus.Failed;
        }

        if (hasPending)
        {
            return MailWakeTargetStatus.Pending;
        }

        return targetStatuses[0];
    }
}
