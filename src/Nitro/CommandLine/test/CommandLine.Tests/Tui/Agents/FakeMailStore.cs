using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// An <see cref="IMailStore"/> exposing only <see cref="QueryParticipationThreadsAsync"/>
/// with configurable rows, for tests of the agent detail popover's Mail section. Every
/// other member throws <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeMailStore : IMailStore
{
    /// <summary>
    /// The rows <see cref="QueryParticipationThreadsAsync"/> returns, sliced to the
    /// requested limit; empty by default.
    /// </summary>
    public IReadOnlyList<MailThreadSummary> ParticipationRows { get; set; } = [];

    /// <summary>
    /// The agent and limit the last <see cref="QueryParticipationThreadsAsync"/> call
    /// received, or null before one is made.
    /// </summary>
    public (string Agent, int? Limit)? LastParticipationQuery { get; private set; }

    public Task<IReadOnlyList<MailThreadSummary>> QueryParticipationThreadsAsync(
        string agent, int? limit, CancellationToken cancellationToken)
    {
        LastParticipationQuery = (agent, limit);

        return Task.FromResult<IReadOnlyList<MailThreadSummary>>(
            limit is { } max ? [.. ParticipationRows.Take(max)] : ParticipationRows);
    }

    public string? FindWorkspaceDirectory() => throw new NotSupportedException();

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

    public Task<IReadOnlyList<MailMessage>> QueryWorkspaceMessagesAsync(
        MailWorkspaceFilter filter, CancellationToken cancellationToken)
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

    public Task<IReadOnlyList<MailThreadSummary>> QueryWorkspaceThreadsAsync(
        string? agent, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailMessage>> SearchAsync(string actor, string text, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<int> CountUnreadAsync(string actor, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailMessage>> QuerySentAsync(
        string sender, int? limit, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
