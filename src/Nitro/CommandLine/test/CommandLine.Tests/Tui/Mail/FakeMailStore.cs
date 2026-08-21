using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Mail;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

/// <summary>
/// An in-memory <see cref="IMailStore"/> exercising the query and write
/// surface the mail board model consumes: <see cref="QueryInboxAsync"/>,
/// <see cref="GetThreadMessagesAsync"/>, <see cref="MarkReadAsync"/>,
/// <see cref="MarkUnreadAsync"/>, <see cref="ArchiveAsync"/>,
/// <see cref="SendMessageAsync"/>, and <see cref="ReplyMessageAsync"/>.
/// The recipient-matching writes reuse the production
/// <see cref="MailRecipientView"/> helper the same way the query surface
/// does, so mode-level tests against this fake do not independently prove
/// the real store's SQL-level recipient and reply-recipient semantics;
/// <c>MailModeRealStoreTests</c> covers those against a real
/// <see cref="Services.Mail.MailStore"/>. Every other member throws
/// <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeMailStore : IMailStore
{
    private int _nextId = 1;

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

    /// <summary>
    /// Sends a message: every given recipient becomes a "to" recipient.
    /// Unlike the real store, no unknown-recipient validation happens here;
    /// that store-owned behavior is covered against a real
    /// <see cref="Services.Mail.MailStore"/> instead.
    /// </summary>
    public Task<MailMessage> SendMessageAsync(MailMessageCreation creation, CancellationToken cancellationToken)
    {
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
            Recipients = recipients
        };

        Messages.Add(message);
        return Task.FromResult(message);
    }

    /// <summary>
    /// Replies to a message: the reply's only recipient is the original
    /// message's sender. Unlike the real store, cc recipients and the
    /// actor-exclusion rule are not reproduced here; that store-owned
    /// behavior is covered against a real <see cref="Services.Mail.MailStore"/>
    /// instead.
    /// </summary>
    public Task<MailMessage> ReplyMessageAsync(
        string inReplyToId, string sender, string body, CancellationToken cancellationToken)
    {
        var original = Messages.FirstOrDefault(m => m.Id == inReplyToId)
            ?? throw new ExitException($"Message '{inReplyToId}' not found.");

        var id = NextId();
        var message = new MailMessage
        {
            Id = id,
            ThreadId = original.ThreadId,
            InReplyTo = inReplyToId,
            Sender = sender,
            Subject = original.Subject,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow,
            Recipients = [new MailRecipient { Name = original.Sender, Kind = MailRecipientKinds.To, Ordinal = 0 }]
        };

        Messages.Add(message);
        return Task.FromResult(message);
    }

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
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailMessage>> SearchAsync(string actor, string text, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MailMessage>> QuerySentAsync(string sender, int? limit, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<int> CountUnreadAsync(string actor, CancellationToken cancellationToken)
    {
        var count = Messages.Count(m =>
            MailRecipientView.FindRecipient(m, actor) is not null
            && MailRecipientView.IsUnread(m, actor)
            && !MailRecipientView.IsArchived(m, actor));

        return Task.FromResult(count);
    }
}
