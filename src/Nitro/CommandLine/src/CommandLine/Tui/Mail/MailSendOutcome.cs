using ChilliCream.Nitro.CommandLine.Commands.Mail;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The outcome of a <see cref="MailComposeForm"/> or <see cref="MailReplyForm"/>
/// submission run through <see cref="MailMode"/>'s own async send effect.
/// Unlike the pre-epic design, a store write alone is never reported as an
/// unconditional "Sent": <see cref="Succeeded"/> only ever means the commit
/// landed, and its own <see cref="MailNotificationResult.Status"/> still
/// decides whether <see cref="ToShowToast"/> shows green. <see cref="Stored"/>
/// is a third, intermediate case: posted the moment the commit lands, ahead
/// of the terminal <see cref="Succeeded"/> or <see cref="Reconciled"/>
/// outcome the actor-wake dispatch-and-observe step can take up to
/// <see cref="WakeDispatchPolicy.BatchDeadline"/> longer to reach.
/// </summary>
internal abstract record MailSendOutcome
{
    private MailSendOutcome()
    {
    }

    /// <summary>
    /// The message committed, but the actor-wake dispatch-and-observe step
    /// has not started resolving it yet: an intermediate signal posted right
    /// after the store commit, so <see cref="MailMode"/> can show a truthful
    /// "Stored" toast before that step even begins, rather than leaving the
    /// transient "Sending" toast the only visible state until the terminal
    /// <see cref="Succeeded"/> or <see cref="Reconciled"/> outcome arrives.
    /// </summary>
    public sealed record Stored(MailMessage Message) : MailSendOutcome;

    /// <summary>
    /// The message committed, and the actor-wake dispatch-and-observe step
    /// ran to completion, so <paramref name="Notification"/> is a truthful,
    /// fully-resolved (or fully-observed-pending) lattice status.
    /// </summary>
    public sealed record Succeeded(MailMessage Message, MailNotificationResult Notification) : MailSendOutcome;

    /// <summary>
    /// The message committed, but its wake outcome could not be reconciled
    /// (the dispatch-and-observe step itself was cancelled or faulted for a
    /// reason unrelated to the commit). Never means unsent: only a store
    /// write that itself failed before ever producing a message produces
    /// <see cref="Failed"/>.
    /// </summary>
    public sealed record Reconciled(MailMessage Message) : MailSendOutcome;

    /// <summary>
    /// The store rejected the write before anything committed: no message,
    /// no wake generation. Carries the store's own <see cref="ExitException"/>
    /// message.
    /// </summary>
    public sealed record Failed(string ToastText) : MailSendOutcome;

    /// <summary>
    /// The toast this outcome should show once observed: green only for a
    /// <see cref="Succeeded"/> outcome whose <see cref="MailNotificationResult.Status"/>
    /// is one of <see cref="WakeReceiptAggregator.IsSuccessful"/>'s successful statuses;
    /// every other case (still pending, failed, unresolved, a
    /// rejected write, or the intermediate <see cref="Stored"/> signal) is
    /// styled to reflect that the recipient's wake is not yet, or was never,
    /// confirmed.
    /// </summary>
    public TuiMessage.ShowToast ToShowToast() => this switch
    {
        Stored stored => FormatStored(stored),
        Succeeded succeeded => FormatSucceeded(succeeded),
        Reconciled reconciled =>
            new TuiMessage.ShowToast($"Stored '{reconciled.Message.Id}'. Notification outcome unknown.", ToastStyle.Warn),
        Failed failed => new TuiMessage.ShowToast(failed.ToastText, ToastStyle.Error),
        _ => throw new NotSupportedException()
    };

    private static TuiMessage.ShowToast FormatStored(Stored stored)
    {
        var id = stored.Message.Id;
        var recipients = string.Join(", ", stored.Message.Recipients.Select(r => r.Name));

        // Info, matching the transient "Sending…" toast this one replaces:
        // the commit is a fact, but this is not yet a terminal outcome, so
        // it must never read as green, amber, or red the way Succeeded,
        // Reconciled, and Failed do.
        return new TuiMessage.ShowToast($"Stored '{id}' to {recipients}.", ToastStyle.Info);
    }

    private static TuiMessage.ShowToast FormatSucceeded(Succeeded succeeded)
    {
        var id = succeeded.Message.Id;
        var recipients = string.Join(", ", succeeded.Message.Recipients.Select(r => r.Name));
        var status = succeeded.Notification.Status;

        if (WakeReceiptAggregator.IsSuccessful(status))
        {
            var phrase = status switch
            {
                MailWakeTargetStatus.Satisfied => "the mail was already read",
                MailWakeTargetStatus.Delegated => "a dashboard accepted delivery",
                MailWakeTargetStatus.Skipped => "no wake was requested",
                _ => "the wake delivered"
            };

            return new TuiMessage.ShowToast($"Sent '{id}' to {recipients}: {phrase}.", ToastStyle.Success);
        }

        if (status == MailWakeTargetStatus.Pending)
        {
            return new TuiMessage.ShowToast(
                $"Stored '{id}' to {recipients}. Notification pending.", ToastStyle.Warn);
        }

        var reason = DescribeFailure(succeeded.Notification);
        return new TuiMessage.ShowToast(
            $"Stored '{id}' to {recipients}. Notification failed: {reason}.", ToastStyle.Error);
    }

    /// <summary>
    /// The representative machine reason for a nonzero, non-pending
    /// notification: the first recipient's own last-attempt reason, or the
    /// notification's own status when no recipient carries one.
    /// </summary>
    private static string DescribeFailure(MailNotificationResult notification) =>
        notification.Recipients.FirstOrDefault(recipient => recipient.LastAttempt is not null)?.LastAttempt?.Reason
        ?? notification.Status;
}
