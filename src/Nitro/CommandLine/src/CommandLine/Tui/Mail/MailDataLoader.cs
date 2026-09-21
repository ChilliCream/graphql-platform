using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Loads mailbox messages and thread summaries from the mail store.
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
    /// Loads inbox thread summaries newest activity first, applying the archived
    /// filter when requested. Unread filtering is applied separately by
    /// <see cref="MailState"/>.
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
    /// Loads summaries of threads the actor sent mail in, newest activity first.
    /// </summary>
    public Task<IReadOnlyList<MailThreadSummary>> LoadSentThreadsAsync(
        string actor,
        CancellationToken cancellationToken)
        => store.QuerySentThreadsAsync(actor, cancellationToken);

    /// <summary>
    /// Loads summaries of threads the actor sent or received mail in, newest
    /// activity first.
    /// </summary>
    public Task<IReadOnlyList<MailThreadSummary>> LoadAllThreadsAsync(
        string actor,
        CancellationToken cancellationToken)
        => store.QueryThreadsAsync(actor, cancellationToken);

    /// <summary>
    /// Loads workspace thread summaries newest activity first, limited to threads
    /// the supplied agent sent or received mail in, or all agents when null.
    /// </summary>
    public Task<IReadOnlyList<MailThreadSummary>> LoadWorkspaceThreadsAsync(
        string? agent,
        CancellationToken cancellationToken)
        => store.QueryWorkspaceThreadsAsync(agent, cancellationToken);
}
