using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// An in-memory <see cref="IMailStore"/> exercising exactly the surface
/// <see cref="ChilliCream.Nitro.CommandLine.Tui.Agents.AgentDetailModel"/>
/// consumes (<see cref="QuerySentAsync"/>). Every other member throws
/// <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeMailStore : IMailStore
{
    public List<MailMessage> Messages { get; } = [];

    public Task<IReadOnlyList<MailMessage>> QuerySentAsync(
        string sender, int? limit, CancellationToken cancellationToken)
    {
        var ordered = Messages
            .Where(m => m.Sender == sender)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id, StringComparer.Ordinal)
            .AsEnumerable();

        if (limit is { } cap)
        {
            ordered = ordered.Take(cap);
        }

        return Task.FromResult<IReadOnlyList<MailMessage>>(ordered.ToList());
    }

    public string? FindWorkspaceDirectory()
        => throw new NotSupportedException();

    public Task InitializeWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MailMessage> SendMessageAsync(MailMessageCreation creation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MailMessage> ReplyMessageAsync(
        string inReplyToId, string sender, string body, MailWakePolicy wakePolicy, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MailMessage?> GetMessageAsync(string id, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MailMessage> GetRequiredMessageAsync(string id, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailMessage>> GetThreadMessagesAsync(string threadId, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailMessage>> QueryInboxAsync(MailInboxFilter filter, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailMessage>> QueryWorkspaceMessagesAsync(MailWorkspaceFilter filter, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task MarkReadAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task MarkUnreadAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task ArchiveAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailThreadSummary>> QueryThreadsAsync(string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailThreadSummary>> QueryInboxThreadsAsync(
        string actor, bool includeArchived, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailThreadSummary>> QuerySentThreadsAsync(string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailThreadSummary>> QueryWorkspaceThreadsAsync(string? agent, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailMessage>> SearchAsync(string actor, string text, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<int> CountUnreadAsync(string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
