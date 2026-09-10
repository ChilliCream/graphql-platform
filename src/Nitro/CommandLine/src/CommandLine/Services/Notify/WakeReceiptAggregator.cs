namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Derives one multi-recipient command's status from its individual actor
/// wake results. Each actor has at most one coding-session target. The successful
/// terminal statuses (<see cref="MailWakeTargetStatus.Delivered"/>,
/// <see cref="MailWakeTargetStatus.Satisfied"/>,
/// <see cref="MailWakeTargetStatus.Delegated"/>,
/// <see cref="MailWakeTargetStatus.Skipped"/>) need nothing further from the
/// caller; <see cref="MailWakeTargetStatus.Pending"/>
/// and <see cref="MailWakeTargetStatus.Failed"/> are not successful. A batch
/// with no targets at all (no live connection to address)
/// aggregates to <see cref="MailWakeTargetStatus.Failed"/>: nobody was, or
/// could be, notified. This is not used to roll up multiple connections for
/// one actor; that state no longer exists.
/// </summary>
internal static class WakeReceiptAggregator
{
    /// <summary>
    /// Returns whether the status is terminal and requires no further wake
    /// work from the caller.
    /// </summary>
    public static bool IsSuccessful(string status) => status switch
    {
        MailWakeTargetStatus.Delivered
            or MailWakeTargetStatus.Satisfied
            or MailWakeTargetStatus.Delegated
            or MailWakeTargetStatus.Skipped => true,
        _ => false
    };

    /// <summary>
    /// Combines every recipient's status into one command-level verdict.
    /// <list type="bullet">
    /// <item>No targets at all: <see cref="MailWakeTargetStatus.Skipped"/>,
    /// since an actor with no live session takes no push and pulls its own
    /// mail instead.</item>
    /// <item>At least one unresolved target:
    /// <see cref="MailWakeTargetStatus.Pending"/>.</item>
    /// <item>Every target failed: <see cref="MailWakeTargetStatus.Failed"/>.</item>
    /// <item>At least one failed target: <see cref="MailWakeTargetStatus.Failed"/>.</item>
    /// <item>Every target completed successfully: a deterministic status in
    /// delivery, delegation, satisfaction, skip precedence.</item>
    /// </list>
    /// </summary>
    public static string Aggregate(IReadOnlyList<string> recipientStatuses)
    {
        if (recipientStatuses.Count == 0)
        {
            return MailWakeTargetStatus.Skipped;
        }

        foreach (var status in recipientStatuses)
        {
            if (status == MailWakeTargetStatus.Pending)
            {
                return MailWakeTargetStatus.Pending;
            }
        }

        if (recipientStatuses.Contains(MailWakeTargetStatus.Failed, StringComparer.Ordinal))
        {
            return MailWakeTargetStatus.Failed;
        }

        if (recipientStatuses.Contains(MailWakeTargetStatus.Delivered, StringComparer.Ordinal))
        {
            return MailWakeTargetStatus.Delivered;
        }

        if (recipientStatuses.Contains(MailWakeTargetStatus.Delegated, StringComparer.Ordinal))
        {
            return MailWakeTargetStatus.Delegated;
        }

        if (recipientStatuses.Contains(MailWakeTargetStatus.Satisfied, StringComparer.Ordinal))
        {
            return MailWakeTargetStatus.Satisfied;
        }

        return MailWakeTargetStatus.Skipped;
    }
}
