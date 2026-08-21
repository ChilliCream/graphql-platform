using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The lifecycle write a <see cref="MailActionOutcome"/> resulted from.
/// </summary>
internal enum MailAction
{
    MarkRead,
    MarkUnread,
    Archive
}

/// <summary>
/// The outcome of one <see cref="MailLifecycleActions"/> write against the
/// mail store.
/// </summary>
internal abstract record MailActionOutcome
{
    private MailActionOutcome()
    {
    }

    /// <summary>
    /// The write succeeded.
    /// </summary>
    public sealed record Succeeded(MailAction Action, string ToastText) : MailActionOutcome;

    /// <summary>
    /// The store rejected the write with an <see cref="ExitException"/>.
    /// </summary>
    public sealed record Failed(MailAction Action, string ToastText) : MailActionOutcome;

    /// <summary>
    /// The shell toast this outcome should show: success styled for
    /// <see cref="Succeeded"/>, error styled for <see cref="Failed"/>.
    /// </summary>
    public TuiMessage.ShowToast ToShowToast() => this switch
    {
        Succeeded succeeded => new TuiMessage.ShowToast(succeeded.ToastText, ToastStyle.Success),
        Failed failed => new TuiMessage.ShowToast(failed.ToastText, ToastStyle.Error),
        _ => throw new NotSupportedException()
    };
}

/// <summary>
/// Mark-read, mark-unread, and archive actions for the selected message:
/// builds the archive confirmation dialog and applies each action to the
/// mail store, the same store members the CLI's read, ack, and archive
/// commands call.
/// </summary>
internal static class MailLifecycleActions
{
    /// <summary>
    /// Builds the confirmation dialog for archiving <paramref name="message"/>.
    /// </summary>
    public static ConfirmDialog CreateArchiveDialog(MailMessage message)
        => new($"Archive message '{message.Id}': \"{message.Subject}\"?", "Archive");

    /// <summary>
    /// Marks <paramref name="message"/> read for <paramref name="actor"/>.
    /// </summary>
    public static async Task<MailActionOutcome> MarkReadAsync(
        IMailStore store,
        MailMessage message,
        string actor,
        CancellationToken cancellationToken)
    {
        try
        {
            await store.MarkReadAsync([message.Id], actor, cancellationToken).ConfigureAwait(false);
            return new MailActionOutcome.Succeeded(MailAction.MarkRead, $"Marked '{message.Id}' read.");
        }
        catch (ExitException ex)
        {
            return new MailActionOutcome.Failed(MailAction.MarkRead, ex.Message);
        }
    }

    /// <summary>
    /// Marks <paramref name="message"/> unread for <paramref name="actor"/>.
    /// </summary>
    public static async Task<MailActionOutcome> MarkUnreadAsync(
        IMailStore store,
        MailMessage message,
        string actor,
        CancellationToken cancellationToken)
    {
        try
        {
            await store.MarkUnreadAsync([message.Id], actor, cancellationToken).ConfigureAwait(false);
            return new MailActionOutcome.Succeeded(MailAction.MarkUnread, $"Marked '{message.Id}' unread.");
        }
        catch (ExitException ex)
        {
            return new MailActionOutcome.Failed(MailAction.MarkUnread, ex.Message);
        }
    }

    /// <summary>
    /// Marks <paramref name="message"/> unread when <paramref name="actor"/>
    /// has already read it, or read otherwise.
    /// </summary>
    public static Task<MailActionOutcome> ToggleReadAsync(
        IMailStore store,
        MailMessage message,
        string actor,
        CancellationToken cancellationToken)
        => MailRecipientView.IsUnread(message, actor)
            ? MarkReadAsync(store, message, actor, cancellationToken)
            : MarkUnreadAsync(store, message, actor, cancellationToken);

    /// <summary>
    /// Archives <paramref name="message"/> for <paramref name="actor"/>.
    /// </summary>
    public static async Task<MailActionOutcome> ArchiveAsync(
        IMailStore store,
        MailMessage message,
        string actor,
        CancellationToken cancellationToken)
    {
        try
        {
            await store.ArchiveAsync([message.Id], actor, cancellationToken).ConfigureAwait(false);
            return new MailActionOutcome.Succeeded(MailAction.Archive, $"Archived '{message.Id}'.");
        }
        catch (ExitException ex)
        {
            return new MailActionOutcome.Failed(MailAction.Archive, ex.Message);
        }
    }
}
