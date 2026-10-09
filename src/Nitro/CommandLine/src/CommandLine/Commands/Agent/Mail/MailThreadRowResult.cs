using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

/// <summary>
/// One thread row, as returned by the structured (JSON) output of the
/// <c>threads</c> command.
/// </summary>
internal sealed record MailThreadRowResult
{
    public required string ThreadId { get; init; }
    public required string Subject { get; init; }
    public required IReadOnlyList<string> Participants { get; init; }
    public required int MessageCount { get; init; }
    public required int UnreadCount { get; init; }
    public required DateTimeOffset LastActivityAt { get; init; }

    public static MailThreadRowResult Create(
        MailThreadSummary summary, IReadOnlyList<string> participants)
        => new()
        {
            ThreadId = summary.ThreadId,
            Subject = summary.Subject,
            Participants = participants,
            MessageCount = summary.MessageCount,
            UnreadCount = summary.UnreadCount ?? 0,
            LastActivityAt = summary.LastMessageAt
        };
}
