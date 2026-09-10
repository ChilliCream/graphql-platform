using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

/// <summary>
/// A message's full detail, as returned by the structured (JSON) output of
/// the <c>read</c> command.
/// </summary>
internal sealed record MailMessageDetailResult
{
    public required string Id { get; init; }
    public required string ThreadId { get; init; }
    public string? InReplyTo { get; init; }
    public required string From { get; init; }
    public required IReadOnlyList<string> To { get; init; }
    public required IReadOnlyList<string> Cc { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required bool Read { get; init; }
    public required bool Archived { get; init; }

    public static MailMessageDetailResult Create(MailMessage message, string actor)
    {
        var recipient = message.Recipients.FirstOrDefault(r => r.Name == actor);

        return new MailMessageDetailResult
        {
            Id = message.Id,
            ThreadId = message.ThreadId,
            InReplyTo = message.InReplyTo,
            From = message.Sender,
            To = message.Recipients
                .Where(r => r.Kind == MailRecipientKinds.To)
                .OrderBy(r => r.Ordinal)
                .Select(r => r.Name)
                .ToArray(),
            Cc = message.Recipients
                .Where(r => r.Kind == MailRecipientKinds.Cc)
                .OrderBy(r => r.Ordinal)
                .Select(r => r.Name)
                .ToArray(),
            Subject = message.Subject,
            Body = message.Body,
            CreatedAt = message.CreatedAt,
            Read = recipient?.ReadAt is not null,
            Archived = recipient?.ArchivedAt is not null
        };
    }
}
