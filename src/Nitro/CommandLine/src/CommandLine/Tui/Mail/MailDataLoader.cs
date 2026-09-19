using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Loads the mail board's list and thread panes from the mail store: one
/// load method per <see cref="MailMailbox"/>. <see cref="LoadInboxAsync"/>
/// and <see cref="LoadInboxThreadsAsync"/> each translate a
/// <see cref="MailListFilter"/> into the store's own "include archived"
/// knob, then, for <see cref="MailListFilter.Archived"/>, narrow the result
/// client side to archived-only entries.
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
    /// <see cref="MailListMode.Threads"/>) for the given filter, newest
    /// activity first, mirroring <see cref="LoadInboxAsync"/>.
    /// <see cref="MailListFilter.Unread"/> carries no thread-level narrowing
    /// here: <see cref="MailState"/> applies that client side from
    /// <see cref="MailThreadSummary.UnreadCount"/>.
    /// </summary>
    public async Task<IReadOnlyList<MailThreadSummary>> LoadInboxThreadsAsync(
        string actor,
        MailListFilter filter,
        CancellationToken cancellationToken)
    {
        var includeArchived = filter == MailListFilter.Archived;

        var threads = await store.QueryInboxThreadsAsync(actor, includeArchived, cancellationToken)
            .ConfigureAwait(false);

        return includeArchived
            ? threads.Where(t => (t.ArchivedCount ?? 0) > 0).ToList()
            : threads;
    }

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
