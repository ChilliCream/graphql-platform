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
    /// The maximum number of body characters retained before any truncation ellipsis.
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
    /// The last message body with whitespace collapsed and at most
    /// <see cref="BodyPreviewMaxLength"/> body characters retained.
    /// An ellipsis is appended when truncated.
    /// </summary>
    public required string BodyPreview { get; init; }

    /// <summary>
    /// The actor's unread message count in the thread, including archived messages,
    /// or null for workspace summaries.
    /// </summary>
    public required int? UnreadCount { get; init; }

    /// <summary>
    /// The actor's archived message count in the thread, or null for workspace summaries.
    /// </summary>
    public required int? ArchivedCount { get; init; }
}
