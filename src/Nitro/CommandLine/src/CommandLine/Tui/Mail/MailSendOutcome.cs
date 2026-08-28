using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The outcome of a <see cref="MailComposeForm"/> or <see cref="MailReplyForm"/>
/// submission run through <see cref="MailMode"/>'s own async send effect.
/// <see cref="Succeeded"/> means the commit landed; nudging the recipients
/// is best effort and never reported. <see cref="Stored"/> is an
/// intermediate signal posted the moment the commit lands.
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
    /// <see cref="Succeeded"/> outcome arrives.
    /// </summary>
    public sealed record Stored(MailMessage Message) : MailSendOutcome;

    /// <summary>
    /// The message committed. Nudging its recipients is best effort and
    /// never reported here.
    /// </summary>
    public sealed record Succeeded(MailMessage Message) : MailSendOutcome;

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
    /// The toast this outcome should show once observed: green for a
    /// <see cref="Succeeded"/> commit; a rejected write or the intermediate
    /// <see cref="Stored"/> signal is styled to reflect that the write is
    /// not yet, or was never,
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

        return new TuiMessage.ShowToast($"Sent '{id}' to {recipients}.", ToastStyle.Success);
    }
}
