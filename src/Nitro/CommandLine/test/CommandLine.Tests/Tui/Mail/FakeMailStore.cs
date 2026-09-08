using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

/// <summary>
/// An in-memory <see cref="IMailStore"/> exercising the query and write
/// surface the mail board model consumes: <see cref="QueryInboxAsync"/>,
/// <see cref="QuerySentAsync"/>, <see cref="QueryWorkspaceMessagesAsync"/>,
/// <see cref="GetThreadMessagesAsync"/>, the four thread-rollup queries
/// (<see cref="QueryThreadsAsync"/>, <see cref="QueryInboxThreadsAsync"/>,
/// <see cref="QuerySentThreadsAsync"/>, <see cref="QueryWorkspaceThreadsAsync"/>,
/// built by the shared <see cref="BuildThreadSummaries"/>), <see cref="MarkReadAsync"/>,
/// <see cref="MarkUnreadAsync"/>, <see cref="ArchiveAsync"/>,
/// <see cref="SendMessageAsync"/>, and <see cref="ReplyMessageAsync"/>.
/// The recipient-matching writes reuse the production
/// <see cref="MailRecipientView"/> helper the same way the query surface
/// does, so mode-level tests against this fake do not independently prove
/// the real store's SQL-level recipient, reply-recipient, and thread-rollup
/// semantics; <c>MailModeRealStoreTests</c> covers those against a real
/// <see cref="Services.Mail.MailStore"/>. Every other member throws
/// <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeMailStore : IMailStore
{
    private int _nextId = 1;

    public List<MailMessage> Messages { get; } = [];

    /// <summary>
    /// When set, every <see cref="SendMessageAsync"/> and <see cref="ReplyMessageAsync"/>
    /// call awaits this before committing, for exercising the write while it
    /// is still in flight.
    /// </summary>
    public TaskCompletionSource? SendGate { get; set; }

    /// <summary>
    /// True once a send or reply has reached <see cref="SendGate"/> and is
    /// waiting on it, so a test can hold a write in flight deterministically
    /// instead of racing the effect queue.
    /// </summary>
    public bool SendGateEntered { get; private set; }

    /// <summary>
    /// When set, every <see cref="SendMessageAsync"/> call throws this
    /// instead of writing, for exercising a failure that is not an
    /// <see cref="ExitException"/>: the same way a genuine bug in the real
    /// store would, this reaches <see cref="MailMode"/>'s send effect as a
    /// faulted completion rather than <see cref="MailSendOutcome.Failed"/>.
    /// </summary>
    public Exception? SendFault { get; set; }

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

    /// <summary>
    /// Sends a message: every given recipient becomes a "to" recipient.
    /// Unlike the real store, no unknown-recipient validation happens here;
    /// that store-owned behavior is covered against a real
    /// <see cref="Services.Mail.MailStore"/> instead. When
    /// <see cref="MailMessageCreation.WakePolicy"/> is
    /// <see cref="MailWakePolicy.Enqueue"/>, every recipient gets a
    /// <see cref="MailWakeReceipt"/> (an incrementing generation, mirroring
    /// the real store's own per-recipient counter), matching the shape
    /// <see cref="MailMessage.WakeReceipts"/> carries.
    /// </summary>
    public async Task<MailMessage> SendMessageAsync(MailMessageCreation creation, CancellationToken cancellationToken)
    {
        if (SendGate is { } gate)
        {
            SendGateEntered = true;
            await gate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        if (SendFault is { } fault)
        {
            throw fault;
        }

        if (creation.To.Count == 0)
        {
            throw new ExitException("At least one recipient is required.");
        }

        var id = NextId();
        var recipients = creation.To
            .Select((name, ordinal) => new MailRecipient { Name = name, Kind = MailRecipientKinds.To, Ordinal = ordinal })
            .ToList();

        var message = new MailMessage
        {
            Id = id,
            ThreadId = id,
            Sender = creation.Sender,
            Subject = creation.Subject,
            Body = creation.Body,
            CreatedAt = DateTimeOffset.UtcNow,
            Recipients = recipients,
            WakeReceipts = BuildWakeReceipts(creation.WakePolicy, recipients)
        };

        Messages.Add(message);
        return message;
    }

    /// <summary>
    /// Replies to a message: the reply's only recipient is the original
    /// message's sender. Unlike the real store, cc recipients and the
    /// actor-exclusion rule are not reproduced here; that store-owned
    /// behavior is covered against a real <see cref="Services.Mail.MailStore"/>
    /// instead. See <see cref="SendMessageAsync"/> for <paramref name="wakePolicy"/>.
    /// </summary>
    public async Task<MailMessage> ReplyMessageAsync(
        string inReplyToId, string sender, string body, MailWakePolicy wakePolicy, CancellationToken cancellationToken)
    {
        if (SendGate is { } gate)
        {
            SendGateEntered = true;
            await gate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        var original = Messages.FirstOrDefault(m => m.Id == inReplyToId)
            ?? throw new ExitException($"Message '{inReplyToId}' not found.");

        var id = NextId();
        var recipients = new List<MailRecipient>
        {
            new() { Name = original.Sender, Kind = MailRecipientKinds.To, Ordinal = 0 }
        };

        var message = new MailMessage
        {
            Id = id,
            ThreadId = original.ThreadId,
            InReplyTo = inReplyToId,
            Sender = sender,
            Subject = original.Subject,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow,
            Recipients = recipients,
            WakeReceipts = BuildWakeReceipts(wakePolicy, recipients)
        };

        Messages.Add(message);
        return message;
    }

    private long _nextGeneration = 1;

    private IReadOnlyList<MailWakeReceipt> BuildWakeReceipts(MailWakePolicy wakePolicy, IReadOnlyList<MailRecipient> recipients)
        => wakePolicy == MailWakePolicy.Enqueue
            ? recipients.Select(r => new MailWakeReceipt { Actor = r.Name, Generation = _nextGeneration++ }).ToList()
            : [];

    public Task<MailMessage> GetRequiredMessageAsync(string id, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task MarkReadAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => ApplyToRecipients(messageIds, actor, recipient => recipient with { ReadAt = DateTimeOffset.UtcNow });

    public Task MarkUnreadAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => ApplyToRecipients(messageIds, actor, recipient => recipient with { ReadAt = null });

    public Task ArchiveAsync(IReadOnlyList<string> messageIds, string actor, CancellationToken cancellationToken)
        => ApplyToRecipients(messageIds, actor, recipient => recipient with { ArchivedAt = DateTimeOffset.UtcNow });

    /// <summary>
    /// Applies <paramref name="update"/> to <paramref name="actor"/>'s
    /// recipient row on every message in <paramref name="messageIds"/>,
    /// validating every id is addressed to the actor before any write, the
    /// same all-or-nothing shape the real store's batch writes use.
    /// </summary>
    private Task ApplyToRecipients(
        IReadOnlyList<string> messageIds, string actor, Func<MailRecipient, MailRecipient> update)
    {
        foreach (var id in messageIds)
        {
            var message = Messages.FirstOrDefault(m => m.Id == id)
                ?? throw new ExitException($"Message '{id}' not found.");

            if (MailRecipientView.FindRecipient(message, actor) is null)
            {
                throw new ExitException($"Message '{id}' is not addressed to '{actor}'.");
            }
        }

        foreach (var id in messageIds)
        {
            var index = Messages.FindIndex(m => m.Id == id);
            var message = Messages[index];
            var recipients = message.Recipients
                .Select(r => string.Equals(r.Name, actor, StringComparison.OrdinalIgnoreCase) ? update(r) : r)
                .ToList();

            Messages[index] = message with { Recipients = recipients };
        }

        return Task.CompletedTask;
    }

    private string NextId() => $"m-fake-{_nextId++}";

    public Task<IReadOnlyList<MailThreadSummary>> QueryThreadsAsync(string actor, CancellationToken cancellationToken)
        => Task.FromResult(BuildThreadSummaries(
            m => m.Sender == actor || MailRecipientView.FindRecipient(m, actor) is not null, actor));

    public Task<IReadOnlyList<MailThreadSummary>> QueryInboxThreadsAsync(
        string actor, bool includeArchived, CancellationToken cancellationToken)
        => Task.FromResult(BuildThreadSummaries(
            m => MailRecipientView.FindRecipient(m, actor) is not null
                && (includeArchived || !MailRecipientView.IsArchived(m, actor)),
            actor));

    public Task<IReadOnlyList<MailThreadSummary>> QuerySentThreadsAsync(string actor, CancellationToken cancellationToken)
        => Task.FromResult(BuildThreadSummaries(m => m.Sender == actor, actor));

    public Task<IReadOnlyList<MailThreadSummary>> QueryWorkspaceThreadsAsync(string? agent, CancellationToken cancellationToken)
        => Task.FromResult(BuildThreadSummaries(
            agent is null ? _ => true : m => m.Sender == agent || MailRecipientView.FindRecipient(m, agent) is not null,
            unreadActor: null));

    /// <summary>
    /// Mirrors <c>MailStore.BuildThreadSummariesAsync</c> in-memory: groups
    /// every message matching <paramref name="threadFilter"/> by thread,
    /// takes the thread's last message for <see cref="MailThreadSummary.LastSender"/>,
    /// <see cref="MailThreadSummary.LastRecipients"/>, and
    /// <see cref="MailThreadSummary.BodyPreview"/>, the root message
    /// (oldest) for <see cref="MailThreadSummary.Subject"/>, and computes
    /// <see cref="MailThreadSummary.UnreadCount"/> and
    /// <see cref="MailThreadSummary.ArchivedCount"/> only when
    /// <paramref name="unreadActor"/> is given - never for another agent's
    /// actor, matching the real store's Workspace-never-actor-scoped rule.
    /// </summary>
    private IReadOnlyList<MailThreadSummary> BuildThreadSummaries(Func<MailMessage, bool> threadFilter, string? unreadActor)
    {
        var threadIds = Messages.Where(threadFilter).Select(m => m.ThreadId).Distinct().ToList();

        var summaries = new List<MailThreadSummary>();

        foreach (var threadId in threadIds)
        {
            var threadMessages = Messages
                .Where(m => m.ThreadId == threadId)
                .OrderBy(m => m.CreatedAt)
                .ThenBy(m => m.Id, StringComparer.Ordinal)
                .ToList();

            if (threadMessages.Count == 0)
            {
                continue;
            }

            var root = threadMessages[0];
            var last = threadMessages[^1];

            var unreadCount = unreadActor is null
                ? (int?)null
                : threadMessages.Count(m => MailRecipientView.IsUnread(m, unreadActor));

            var archivedCount = unreadActor is null
                ? (int?)null
                : threadMessages.Count(m => MailRecipientView.IsArchived(m, unreadActor));

            summaries.Add(new MailThreadSummary
            {
                ThreadId = threadId,
                Subject = root.Subject,
                MessageCount = threadMessages.Count,
                LastMessageAt = last.CreatedAt,
                LastSender = last.Sender,
                LastRecipients = last.Recipients.OrderBy(r => r.Ordinal).Select(r => r.Name).ToArray(),
                BodyPreview = last.Body,
                UnreadCount = unreadCount,
                ArchivedCount = archivedCount
            });
        }

        return summaries
            .OrderByDescending(s => s.LastMessageAt)
            .ThenByDescending(s => s.ThreadId, StringComparer.Ordinal)
            .ToList();
    }

    public Task<IReadOnlyList<MailMessage>> SearchAsync(string actor, string text, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailMessage>> QuerySentAsync(
        string sender, int? limit, CancellationToken cancellationToken)
    {
        var ordered = Messages
            .Where(m => m.Sender == sender)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id, StringComparer.Ordinal)
            .AsEnumerable();

        if (limit is { } value)
        {
            ordered = ordered.Take(value);
        }

        return Task.FromResult<IReadOnlyList<MailMessage>>(ordered.ToList());
    }

    public Task<IReadOnlyList<MailMessage>> QueryWorkspaceMessagesAsync(
        MailWorkspaceFilter filter, CancellationToken cancellationToken)
    {
        IEnumerable<MailMessage> query = Messages;

        if (filter.Agent is { } agent)
        {
            query = query.Where(m => m.Sender == agent || MailRecipientView.FindRecipient(m, agent) is not null);
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

    public Task<int> CountUnreadAsync(string actor, CancellationToken cancellationToken)
    {
        var count = Messages.Count(m =>
            MailRecipientView.FindRecipient(m, actor) is not null
            && MailRecipientView.IsUnread(m, actor)
            && !MailRecipientView.IsArchived(m, actor));

        return Task.FromResult(count);
    }
}
