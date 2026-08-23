using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Loads the mail board's list and thread panes from the mail store: one
/// load method per <see cref="MailMailbox"/>. <see cref="LoadInboxAsync"/>
/// translates a <see cref="MailListFilter"/> into a <see cref="MailInboxFilter"/>
/// query, deriving the archived-only filter client side since the store
/// exposes only "include archived", not "archived only"; the other mailboxes
/// carry no such filter. Issues no SQL of its own.
/// </summary>
internal sealed class MailDataLoader(IMailStore store)
{
    /// <summary>
    /// Loads the actor's inbox for the given filter, newest first.
    /// </summary>
    public async Task<IReadOnlyList<MailMessage>> LoadInboxAsync(
        string actor,
        MailListFilter filter,
        CancellationToken cancellationToken)
    {
        var storeFilter = new MailInboxFilter
        {
            Actor = actor,
            UnreadOnly = filter == MailListFilter.Unread,
            IncludeArchived = filter == MailListFilter.Archived
        };

        var messages = await store.QueryInboxAsync(storeFilter, cancellationToken).ConfigureAwait(false);

        return filter == MailListFilter.Archived
            ? messages.Where(m => MailRecipientView.IsArchived(m, actor)).ToList()
            : messages;
    }

    /// <summary>
    /// Loads every message the actor sent, regardless of recipient, newest
    /// first.
    /// </summary>
    public Task<IReadOnlyList<MailMessage>> LoadSentAsync(
        string actor,
        CancellationToken cancellationToken)
        => store.QuerySentAsync(actor, limit: null, cancellationToken);

    /// <summary>
    /// Loads every message the actor sent or received, newest first.
    /// </summary>
    public Task<IReadOnlyList<MailMessage>> LoadAllAsync(
        string actor,
        CancellationToken cancellationToken)
        => store.QueryWorkspaceMessagesAsync(
            new MailWorkspaceFilter { Agent = actor }, cancellationToken);

    /// <summary>
    /// Loads every message in the workspace, newest first, narrowed to
    /// messages <paramref name="agent"/> sent or received when given, or
    /// across every agent when null.
    /// </summary>
    public Task<IReadOnlyList<MailMessage>> LoadWorkspaceAsync(
        string? agent,
        CancellationToken cancellationToken)
        => store.QueryWorkspaceMessagesAsync(new MailWorkspaceFilter { Agent = agent }, cancellationToken);

    /// <summary>
    /// Loads every message in the given thread, oldest first.
    /// </summary>
    public Task<IReadOnlyList<MailMessage>> LoadThreadAsync(
        string threadId,
        CancellationToken cancellationToken)
        => store.GetThreadMessagesAsync(threadId, cancellationToken);

    /// <summary>
    /// Loads the actor's inbox thread rollups (the "Inbox" mailbox scope in
    /// <see cref="MailListMode.Threads"/>), newest activity first. Unlike
    /// <see cref="LoadInboxAsync"/>, this carries no <see cref="MailListFilter"/>:
    /// the store exposes no filtered thread query, so Threads mode within
    /// Inbox always shows the full inbox thread set (see <see cref="MailState"/>).
    /// </summary>
    public Task<IReadOnlyList<MailThreadSummary>> LoadInboxThreadsAsync(
        string actor,
        CancellationToken cancellationToken)
        => store.QueryInboxThreadsAsync(actor, cancellationToken);

    /// <summary>
    /// Loads thread rollups for every thread the actor sent a message in
    /// (the "Sent" mailbox scope in <see cref="MailListMode.Threads"/>),
    /// newest activity first.
    /// </summary>
    public Task<IReadOnlyList<MailThreadSummary>> LoadSentThreadsAsync(
        string actor,
        CancellationToken cancellationToken)
        => store.QuerySentThreadsAsync(actor, cancellationToken);

    /// <summary>
    /// Loads thread rollups for every thread the actor sent or received a
    /// message in (the "All" mailbox scope in <see cref="MailListMode.Threads"/>),
    /// newest activity first.
    /// </summary>
    public Task<IReadOnlyList<MailThreadSummary>> LoadAllThreadsAsync(
        string actor,
        CancellationToken cancellationToken)
        => store.QueryThreadsAsync(actor, cancellationToken);

    /// <summary>
    /// Loads thread rollups for every thread in the workspace (the
    /// "Workspace" mailbox scope in <see cref="MailListMode.Threads"/>),
    /// newest activity first, narrowed to threads <paramref name="agent"/>
    /// sent or received a message in when given.
    /// </summary>
    public Task<IReadOnlyList<MailThreadSummary>> LoadWorkspaceThreadsAsync(
        string? agent,
        CancellationToken cancellationToken)
        => store.QueryWorkspaceThreadsAsync(agent, cancellationToken);
}
