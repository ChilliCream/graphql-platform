using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Editing;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Mark-read, mark-unread, and archive actions for the selected message:
/// builds the archive confirmation dialog and applies each action to the
/// mail store. Also owns the shared "refuse-with-reason" text
/// <see cref="MailMode"/> shows for every mutating gesture, u, a, c, and r,
/// while <see cref="MailMailbox.Workspace"/> is the active mailbox.
/// </summary>
internal static class MailLifecycleActions
{
    /// <summary>
    /// The toast shown when <see cref="IsReadOnly"/> refuses a mutating
    /// gesture, u, a, c, or r, before it ever reaches the store.
    /// </summary>
    public const string WorkspaceReadOnlyMessage =
        "Workspace is read-only. Press Shift+I for Inbox to make changes.";

    /// <summary>
    /// Whether <paramref name="mailbox"/> refuses every mutating gesture
    /// under <paramref name="actor"/>: always when it is null, and
    /// otherwise when <paramref name="mailbox"/> is <see cref="MailMailbox.Workspace"/>.
    /// </summary>
    public static bool IsReadOnly(MailMailbox mailbox, string? actor)
        => actor is null || mailbox == MailMailbox.Workspace;

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
