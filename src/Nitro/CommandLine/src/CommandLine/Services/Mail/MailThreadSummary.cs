namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// A thread's rollup, as returned by <see cref="IMailStore.QueryThreadsAsync"/>.
/// </summary>
internal sealed record MailThreadSummary
{
    public required string ThreadId { get; init; }

    /// <summary>
    /// The subject of the thread's root message.
    /// </summary>
    public required string Subject { get; init; }

    public required int MessageCount { get; init; }
    public required DateTimeOffset LastMessageAt { get; init; }
    public required string LastSender { get; init; }

    /// <summary>
    /// How many messages in the thread the queried actor has not yet read.
    /// </summary>
    public required int UnreadCount { get; init; }
}
