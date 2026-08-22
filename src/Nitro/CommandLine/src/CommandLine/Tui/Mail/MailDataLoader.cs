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
    /// Loads every message in the workspace, across every agent, newest
    /// first.
    /// </summary>
    public Task<IReadOnlyList<MailMessage>> LoadWorkspaceAsync(
        CancellationToken cancellationToken)
        => store.QueryWorkspaceMessagesAsync(new MailWorkspaceFilter(), cancellationToken);

    /// <summary>
    /// Loads every message in the given thread, oldest first.
    /// </summary>
    public Task<IReadOnlyList<MailMessage>> LoadThreadAsync(
        string threadId,
        CancellationToken cancellationToken)
        => store.GetThreadMessagesAsync(threadId, cancellationToken);
}
