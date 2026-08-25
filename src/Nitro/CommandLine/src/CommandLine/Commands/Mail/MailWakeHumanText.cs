using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

/// <summary>
/// Formats the human-readable wake outcome send, reply, and broadcast share,
/// on top of each command's own "Sent"/notes lines.
/// </summary>
internal static class MailWakeHumanText
{
    /// <summary>
    /// Writes the zero-exit wake summary line to stdout, following the
    /// command's own "Sent" line: what delivered, what was no longer needed,
    /// or that a dashboard accepted responsibility. Writes nothing for
    /// <see cref="MailWakeTargetStatus.Skipped"/>, the explicit no-op the
    /// caller itself asked for with <c>--no-ping</c>.
    /// </summary>
    public static void WriteDelivered(INitroConsole console, MailNotificationResult notification)
    {
        var phrase = notification.Status switch
        {
            MailWakeTargetStatus.Delivered => "wake delivered.",
            MailWakeTargetStatus.Satisfied => "wake no longer needed because the mail was read.",
            MailWakeTargetStatus.Delegated => "dashboard accepted responsibility for delivery.",
            _ => null
        };

        if (phrase is not null)
        {
            console.WriteLine(phrase);
        }
    }

    /// <summary>
    /// Writes the full nonzero-exit outcome to stderr: <paramref name="message"/>
    /// is durably stored (the message ID and recipients are echoed so it is
    /// never lost), but at least one recipient's wake did not deliver or get
    /// accepted. Follows with one deterministic line per recipient whose wake
    /// did not reach a zero status.
    /// </summary>
    public static void WriteStoredButUnconfirmed(
        INitroConsole console,
        MailMessage message,
        MailNotificationResult notification,
        IReadOnlyList<string> unregistered)
    {
        console.Error.WriteErrorLine(
            $"Stored '{message.Id.EscapeMarkup()}' to "
            + $"{string.Join(", ", message.Recipients.Select(recipient => recipient.Name)).EscapeMarkup()}.");

        foreach (var name in unregistered)
        {
            console.Error.WriteErrorLine($"note: '{name.EscapeMarkup()}' has never registered.");
        }

        console.Error.WriteErrorLine(
            notification.Status == MailWakeTargetStatus.Pending
                ? "message stored but wake remains unconfirmed."
                : $"message stored, but wake failed: {DescribeFailure(notification).EscapeMarkup()}.");

        foreach (var recipient in notification.Recipients)
        {
            if (recipient.Status is MailWakeTargetStatus.Delivered
                or MailWakeTargetStatus.Satisfied
                or MailWakeTargetStatus.Delegated
                or MailWakeTargetStatus.Skipped)
            {
                continue;
            }

            var detail = recipient.LastAttempt is null
                ? recipient.Status
                : $"{recipient.Status} ({recipient.LastAttempt.Reason})";

            console.Error.WriteErrorLine($"  {recipient.Actor.EscapeMarkup()}: {detail.EscapeMarkup()}");
        }
    }

    /// <summary>
    /// The representative machine reason for a <see cref="MailWakeTargetStatus.Failed"/>
    /// or <see cref="WakeReceiptAggregator.Partial"/> notification: the first
    /// recipient's <see cref="MailWakeRecipientResult.LastAttempt"/> reason,
    /// or the notification's own status when no recipient carries one.
    /// </summary>
    private static string DescribeFailure(MailNotificationResult notification) =>
        notification.Recipients.FirstOrDefault(recipient => recipient.LastAttempt is not null)
            ?.LastAttempt?.Reason
        ?? notification.Status;
}
