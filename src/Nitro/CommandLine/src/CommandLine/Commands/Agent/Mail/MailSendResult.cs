using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

/// <summary>
/// A sent message's core fields, as returned by the structured (JSON) output
/// of the <c>send</c> command.
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
    /// Always true: this result exists only after the message durably committed.
    /// </summary>
    public required bool MessageStored { get; init; }

    public static MailSendResult Create(MailMessage message) => new()
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
        MessageStored = true
    };
}
