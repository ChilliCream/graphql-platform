namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Combines recipient wake statuses and identifies successful terminal statuses.
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
    /// Returns the first status present in pending, failed, delivered, delegated, then
    /// satisfied precedence. Returns skipped when none of those statuses are present,
    /// including for empty input.
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
