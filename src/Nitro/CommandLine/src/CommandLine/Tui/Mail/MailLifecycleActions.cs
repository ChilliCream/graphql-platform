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
/// commands call. Also owns the shared "refuse-with-reason" text
/// <see cref="MailMode"/> shows for every mutating gesture, u, a, c, and
/// r, while <see cref="MailMailbox.Workspace"/> is the active mailbox,
/// rather than each gesture reaching this class's own attempt-and-catch
/// store calls (below) or the store's <c>ResolveReplyAsync</c> check only
/// to fail.
/// </summary>
internal static class MailLifecycleActions
{
    /// <summary>
    /// The toast shown when <see cref="IsReadOnly"/> refuses a mutating
    /// gesture, u, a, c, or r, before it ever reaches the store.
    /// <see cref="MailMailbox.Workspace"/> shows every agent's mail, so it
    /// is read-only by default rather than by warning: this narrows the
    /// mode's capability outright, so muscle memory from the personal
    /// mailboxes cannot do damage, instead of letting a mutation fail
    /// against the store's <c>ValidateRecipientOwnershipAsync</c> or
    /// <c>ResolveReplyAsync</c> authorization checks and surface as a raw
    /// <see cref="ExitException"/> message the user did not ask for. This
    /// applies uniformly, including to reply on a thread the actor
    /// participates in and would otherwise be allowed to answer: Workspace
    /// stays a single predictable read-only mode rather than one whose
    /// gestures work or fail per message. If a mutating action is ever
    /// allowed here later, its confirmation must name the message's
    /// owner (for example "Archive message in 'planner's mailbox?"), not
    /// the actor, since most messages Workspace shows are not the actor's.
    /// </summary>
    public const string WorkspaceReadOnlyMessage =
        "Workspace is read-only. Press Shift+I for Inbox to make changes.";

    /// <summary>
    /// Whether <paramref name="mailbox"/> refuses every mutating gesture,
    /// showing <see cref="WorkspaceReadOnlyMessage"/> instead of reaching
    /// the store or opening a form or confirmation. See
    /// <see cref="WorkspaceReadOnlyMessage"/> for why.
    /// </summary>
    /// <remarks>
    /// Because this is currently the only gate on a Workspace-wide action,
    /// no bulk action exists yet that needs to state its target explicitly.
    /// Workspace also carries a per-agent filter (<see cref="MailState.AgentFilter"/>,
    /// applied through <see cref="MailMailbox.Workspace"/> only), so once a
    /// bulk action is added here it must name the active filter and the
    /// affected count in its confirmation text (for example "Archive 4
    /// messages for 'bob'?" vs. "Archive 12 messages?"), since a filtered
    /// list makes "all" ambiguous between "everything shown" and "every
    /// message in Workspace". Mailpit special-cased this with a distinct
    /// "Delete all messages matching search" action rather than overloading
    /// its plain "Delete all".
    /// </remarks>
    public static bool IsReadOnly(MailMailbox mailbox) => mailbox == MailMailbox.Workspace;

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
