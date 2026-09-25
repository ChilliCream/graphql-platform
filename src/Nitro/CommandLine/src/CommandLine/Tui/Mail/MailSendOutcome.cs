using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The storage outcome of a compose or reply submission.
/// <see cref="Stored"/> is an intermediate commit notice; recipient notification
/// is separate from storage success.
/// </summary>
internal abstract record MailSendOutcome
{
    private MailSendOutcome()
    {
    }

    /// <summary>
    /// An intermediate notice that the message and its wake intent were committed.
    /// </summary>
    public sealed record Stored(MailMessage Message) : MailSendOutcome;

    /// <summary>
    /// The message was committed; this does not report recipient notification.
    /// </summary>
    public sealed record Succeeded(MailMessage Message) : MailSendOutcome;

    /// <summary>
    /// The message was committed, but its notification outcome is unknown.
    /// </summary>
    public sealed record Reconciled(MailMessage Message) : MailSendOutcome;

    /// <summary>
    /// The store rejected the write before anything committed: no message,
    /// no wake generation. Carries the store's own <see cref="ExitException"/>
    /// message.
    /// </summary>
    public sealed record Failed(string ToastText) : MailSendOutcome;

    /// <summary>
    /// Returns an informational stored toast, a successful send toast, a warning for
    /// an unknown notification outcome, or an error for a rejected write.
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

        // Info: not yet a terminal outcome.
        return new TuiMessage.ShowToast($"Stored '{id}' to {recipients}.", ToastStyle.Info);
    }

    private static TuiMessage.ShowToast FormatSucceeded(Succeeded succeeded)
    {
        var id = succeeded.Message.Id;
        var recipients = string.Join(", ", succeeded.Message.Recipients.Select(r => r.Name));

        return new TuiMessage.ShowToast($"Sent '{id}' to {recipients}.", ToastStyle.Success);
    }
}
