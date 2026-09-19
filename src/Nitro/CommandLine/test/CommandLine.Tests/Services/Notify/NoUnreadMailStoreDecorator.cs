using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

/// <summary>
/// Delegates every <see cref="IMailStore"/> member to <paramref
/// name="inner"/> except <see cref="QueryInboxAsync"/> and <see
/// cref="CountUnreadAsync"/>, which always report no unread mail: wiring
/// this into <see cref="PingSessionExecutor"/>'s own digest lookup, while
/// <see cref="ActorWakeDispatcher"/> keeps the real <paramref name="inner"/>
/// for its own outstanding-mail gate, deterministically reproduces the
/// health-only race the executor's opencode branch handles (the mail a wake
/// targeted got read out from under it by the time the digest was built).
/// </summary>
internal sealed class NoUnreadMailStoreDecorator(IMailStore inner) : IMailStore
{
    public string? FindWorkspaceDirectory() => inner.FindWorkspaceDirectory();

    public Task InitializeWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken)
        => inner.InitializeWorkspaceAsync(workspaceDirectory, cancellationToken);

    public Task<MailMessage> SendMessageAsync(MailMessageCreation creation, CancellationToken cancellationToken)
        => inner.SendMessageAsync(creation, cancellationToken);

    public Task<MailMessage> ReplyMessageAsync(
        string inReplyToId, string sender, string body, MailWakePolicy wakePolicy,
        CancellationToken cancellationToken)
        => inner.ReplyMessageAsync(inReplyToId, sender, body, wakePolicy, cancellationToken);

    public Task<MailMessage?> GetMessageAsync(string id, CancellationToken cancellationToken)
        => inner.GetMessageAsync(id, cancellationToken);

    public Task<MailMessage> GetRequiredMessageAsync(string id, CancellationToken cancellationToken)
        => inner.GetRequiredMessageAsync(id, cancellationToken);

    public Task<IReadOnlyList<MailMessage>> GetThreadMessagesAsync(
        string threadId, CancellationToken cancellationToken)
        => inner.GetThreadMessagesAsync(threadId, cancellationToken);

    public Task<IReadOnlyList<MailMessage>> QueryInboxAsync(MailInboxFilter filter, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<MailMessage>>([]);

    public Task<IReadOnlyList<MailMessage>> QueryWorkspaceMessagesAsync(
        MailWorkspaceFilter filter, CancellationToken cancellationToken)
        => inner.QueryWorkspaceMessagesAsync(filter, cancellationToken);

    public Task MarkReadAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => inner.MarkReadAsync(messageIds, actor, cancellationToken);

    public Task MarkUnreadAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => inner.MarkUnreadAsync(messageIds, actor, cancellationToken);

    public Task ArchiveAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => inner.ArchiveAsync(messageIds, actor, cancellationToken);

    public Task<IReadOnlyList<MailThreadSummary>> QueryThreadsAsync(string actor, CancellationToken cancellationToken)
        => inner.QueryThreadsAsync(actor, cancellationToken);

    public Task<IReadOnlyList<MailThreadSummary>> QueryInboxThreadsAsync(
        string actor, bool includeArchived, CancellationToken cancellationToken)
        => inner.QueryInboxThreadsAsync(actor, includeArchived, cancellationToken);

    public Task<IReadOnlyList<MailThreadSummary>> QuerySentThreadsAsync(
        string actor, CancellationToken cancellationToken)
        => inner.QuerySentThreadsAsync(actor, cancellationToken);

    public Task<IReadOnlyList<MailThreadSummary>> QueryWorkspaceThreadsAsync(
        string? agent, CancellationToken cancellationToken)
        => inner.QueryWorkspaceThreadsAsync(agent, cancellationToken);

    public Task<IReadOnlyList<MailMessage>> SearchAsync(string actor, string text, CancellationToken cancellationToken)
        => inner.SearchAsync(actor, text, cancellationToken);

    public Task<int> CountUnreadAsync(string actor, CancellationToken cancellationToken)
        => Task.FromResult(0);

    public Task<IReadOnlyList<MailMessage>> QuerySentAsync(
        string sender, int? limit, CancellationToken cancellationToken)
        => inner.QuerySentAsync(sender, limit, cancellationToken);
}
