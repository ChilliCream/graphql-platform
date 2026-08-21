using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Loads the mail board's list and thread panes from the mail store:
/// translates a <see cref="MailListFilter"/> into a <see cref="MailInboxFilter"/>
/// query, deriving the archived-only filter client side since the store
/// exposes only "include archived", not "archived only". Issues no SQL of
/// its own.
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
    /// Loads every message in the given thread, oldest first.
    /// </summary>
    public Task<IReadOnlyList<MailMessage>> LoadThreadAsync(
        string threadId,
        CancellationToken cancellationToken)
        => store.GetThreadMessagesAsync(threadId, cancellationToken);
}
