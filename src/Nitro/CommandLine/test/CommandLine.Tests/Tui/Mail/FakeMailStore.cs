using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

/// <summary>
/// An in-memory <see cref="IMailStore"/> exercising the query surface the
/// mail board model consumes (<see cref="QueryInboxAsync"/> and
/// <see cref="GetThreadMessagesAsync"/>). Every other member throws
/// <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeMailStore : IMailStore
{
    public List<MailMessage> Messages { get; } = [];

    public Task<IReadOnlyList<MailMessage>> QueryInboxAsync(
        MailInboxFilter filter,
        CancellationToken cancellationToken)
    {
        IEnumerable<MailMessage> query = Messages.Where(m => MailRecipientView.FindRecipient(m, filter.Actor) is not null);

        if (filter.UnreadOnly)
        {
            query = query.Where(m => MailRecipientView.IsUnread(m, filter.Actor));
        }

        if (!filter.IncludeArchived)
        {
            query = query.Where(m => !MailRecipientView.IsArchived(m, filter.Actor));
        }

        if (filter.From is { } from)
        {
            query = query.Where(m => m.Sender == from);
        }

        if (filter.Since is { } since)
        {
            query = query.Where(m => m.CreatedAt >= since);
        }

        var ordered = query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id, StringComparer.Ordinal)
            .AsEnumerable();

        if (filter.Limit is { } limit)
        {
            ordered = ordered.Take(limit);
        }

        return Task.FromResult<IReadOnlyList<MailMessage>>(ordered.ToList());
    }

    public Task<IReadOnlyList<MailMessage>> GetThreadMessagesAsync(
        string threadId,
        CancellationToken cancellationToken)
    {
        var result = Messages
            .Where(m => m.ThreadId == threadId)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult<IReadOnlyList<MailMessage>>(result);
    }

    public Task<MailMessage?> GetMessageAsync(string id, CancellationToken cancellationToken)
        => Task.FromResult(Messages.FirstOrDefault(m => m.Id == id));

    public string? FindWorkspaceDirectory() => throw new NotSupportedException();

    public Task InitializeWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MailAgent> RegisterAgentAsync(string name, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MailAgent?> GetAgentAsync(string name, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailAgent>> GetAgentsAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MailMessage> SendMessageAsync(MailMessageCreation creation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MailMessage> ReplyMessageAsync(
        string inReplyToId, string sender, string body, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MailMessage> GetRequiredMessageAsync(string id, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task MarkReadAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task MarkUnreadAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task ArchiveAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailThreadSummary>> QueryThreadsAsync(string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailMessage>> SearchAsync(string actor, string text, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<int> CountUnreadAsync(string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
