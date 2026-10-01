using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

/// <summary>
/// An in-memory <see cref="IMailStore"/> supporting <see cref="MailMode"/> queries and writes.
/// Unsupported members throw <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeMailStore : IMailStore
{
    private int _nextId = 1;

    public List<MailMessage> Messages { get; } = [];

    /// <summary>
    /// When set, every <see cref="SendMessageAsync"/> and <see cref="ReplyMessageAsync"/>
    /// call awaits this before committing.
    /// </summary>
    public TaskCompletionSource? SendGate { get; set; }

    /// <summary>
    /// True after a send or reply has entered <see cref="SendGate"/>; remains true after the wait ends.
    /// </summary>
    public bool SendGateEntered { get; private set; }

    /// <summary>
    /// When set, every <see cref="SendMessageAsync"/> call throws this instead of writing.
    /// </summary>
    public Exception? SendFault { get; set; }

    public Task<IReadOnlyList<MailMessage>> QueryInboxAsync(
        MailInboxFilter filter,
        CancellationToken cancellationToken)
    {
        var query = Messages.Where(m => MailRecipientView.FindRecipient(m, filter.Actor) is not null);

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
    /// Sends to the names in <see cref="MailMessageCreation.To"/> without actor lookup;
    /// CC recipients are ignored. <see cref="MailWakePolicy.Enqueue"/> creates one
    /// <see cref="MailWakeReceipt"/> per recipient with a generation from a shared counter.
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
    /// Replies only to the original sender. <see cref="MailWakePolicy.Enqueue"/> creates
    /// a wake receipt using the same generation counter as <see cref="SendMessageAsync"/>.
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
    /// Applies <paramref name="update"/> to <paramref name="actor"/>'s recipient row on every
    /// message in <paramref name="messageIds"/>, validating every id is addressed to the actor
    /// before any write.
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
    /// Returns summaries for threads with at least one message matching <paramref name="threadFilter"/>.
    /// Each summary includes the entire thread, with unread and archived counts null
    /// when <paramref name="unreadActor"/> is null.
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
