using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Editing;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Applies read-state and archive changes and builds archive confirmations.
/// Store rejections represented by <see cref="ExitException"/> become failed outcomes.
/// </summary>
internal static class MailLifecycleActions
{
    /// <summary>
    /// The refusal toast for writes attempted in the Workspace mailbox.
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
    /// Marks an unread recipient copy read, or marks it unread otherwise.
    /// The store rejects an actor who is not a recipient.
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
