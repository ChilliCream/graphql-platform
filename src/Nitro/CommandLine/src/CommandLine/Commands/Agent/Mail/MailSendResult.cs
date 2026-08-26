using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

/// <summary>
/// A sent message's core fields, as returned by the structured (JSON) output
/// of the <c>send</c> command. Unlike <see cref="MailMessageResult"/>, this
/// carries the recipients that have never registered.
/// </summary>
internal sealed record MailSendResult
{
    public required string Id { get; init; }
    public required string ThreadId { get; init; }
    public string? InReplyTo { get; init; }
    public required string From { get; init; }
    public required IReadOnlyList<string> To { get; init; }
    public required IReadOnlyList<string> Cc { get; init; }
    public required string Subject { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The normalized names, in recipient order, of every recipient that has
    /// never registered. Empty when every recipient is registered.
    /// </summary>
    public required IReadOnlyList<string> Unregistered { get; init; }

    /// <summary>
    /// Always true: this result exists only after the message durably
    /// committed. Storage failures never produce a <see cref="MailSendResult"/>;
    /// they surface through the ordinary command error path instead.
    /// </summary>
    public required bool MessageStored { get; init; }

    /// <summary>
    /// The actor-wake notification outcome for this message's recipients,
    /// separate from and never overriding <see cref="MessageStored"/>.
    /// </summary>
    public required MailNotificationResult Notification { get; init; }

    public static MailSendResult Create(MailMessage message, MailNotificationResult notification) => new()
    {
        Id = message.Id,
        ThreadId = message.ThreadId,
        InReplyTo = message.InReplyTo,
        From = message.Sender,
        To = message.Recipients
            .Where(recipient => recipient.Kind == MailRecipientKinds.To)
            .OrderBy(recipient => recipient.Ordinal)
            .Select(recipient => recipient.Name)
            .ToArray(),
        Cc = message.Recipients
            .Where(recipient => recipient.Kind == MailRecipientKinds.Cc)
            .OrderBy(recipient => recipient.Ordinal)
            .Select(recipient => recipient.Name)
            .ToArray(),
        Subject = message.Subject,
        CreatedAt = message.CreatedAt,
        Unregistered = message.Unregistered,
        MessageStored = true,
        Notification = notification
    };
}
