using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

/// <summary>
/// One inbox row, as returned by the structured (JSON) output of the
/// <c>inbox</c> command.
/// </summary>
internal sealed record MailInboxRowResult
{
    public required string Id { get; init; }
    public required string ThreadId { get; init; }
    public required string From { get; init; }
    public required string Subject { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required bool Read { get; init; }
    public required bool Archived { get; init; }

    public static MailInboxRowResult Create(MailMessage message, string actor)
    {
        var recipient = message.Recipients.FirstOrDefault(r => r.Name == actor);

        return new MailInboxRowResult
        {
            Id = message.Id,
            ThreadId = message.ThreadId,
            From = message.Sender,
            Subject = message.Subject,
            CreatedAt = message.CreatedAt,
            Read = recipient?.ReadAt is not null,
            Archived = recipient?.ArchivedAt is not null
        };
    }
}
