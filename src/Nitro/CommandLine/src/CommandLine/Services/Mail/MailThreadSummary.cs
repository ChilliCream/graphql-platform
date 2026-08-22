namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// A thread's rollup, as returned by <see cref="IMailStore.QueryThreadsAsync"/>,
/// <see cref="IMailStore.QueryInboxThreadsAsync"/>,
/// <see cref="IMailStore.QuerySentThreadsAsync"/>, and
/// <see cref="IMailStore.QueryWorkspaceThreadsAsync"/>.
/// </summary>
internal sealed record MailThreadSummary
{
    /// <summary>
    /// The maximum length of <see cref="BodyPreview"/> before truncation.
    /// </summary>
    public const int BodyPreviewMaxLength = 140;

    public required string ThreadId { get; init; }

    /// <summary>
    /// The subject of the thread's root message.
    /// </summary>
    public required string Subject { get; init; }

    public required int MessageCount { get; init; }
    public required DateTimeOffset LastMessageAt { get; init; }

    /// <summary>
    /// The sender of the thread's last message.
    /// </summary>
    public required string LastSender { get; init; }

    /// <summary>
    /// The recipient names, to and cc combined, of the thread's last
    /// message (not the thread root's).
    /// </summary>
    public required IReadOnlyList<string> LastRecipients { get; init; }

    /// <summary>
    /// The thread's last message body, whitespace-collapsed (runs of spaces,
    /// tabs, and newlines become a single space) and truncated to
    /// <see cref="BodyPreviewMaxLength"/> characters with a trailing "…" when
    /// truncated.
    /// </summary>
    public required string BodyPreview { get; init; }

    /// <summary>
    /// How many messages in the thread the queried actor has not yet read,
    /// or null when the rollup is not scoped to an actor: workspace rollups
    /// never carry an actor's read state, including another agent's.
    /// </summary>
    public required int? UnreadCount { get; init; }
}
