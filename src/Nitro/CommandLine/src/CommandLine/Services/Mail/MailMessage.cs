namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// A message and its recipients. Null <see cref="InReplyTo"/> means the
/// message starts its own thread.
/// </summary>
internal sealed record MailMessage
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the messages table.
    /// </summary>
    public const string Columns =
        "id AS Id, thread_id AS ThreadId, in_reply_to AS InReplyTo, sender AS Sender, "
        + "subject AS Subject, body AS Body, created_at AS CreatedAt";

    public required string Id { get; init; }
    public required string ThreadId { get; init; }
    public string? InReplyTo { get; init; }
    public required string Sender { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Every recipient, to and cc combined, ordered by ordinal.
    /// </summary>
    public IReadOnlyList<MailRecipient> Recipients { get; init; } = [];

    /// <summary>
    /// The normalized names, in recipient order, of every recipient that has
    /// never registered: their agent row is marked implicit, whether it was
    /// already implicit or was created by this send. Populated only by
    /// <see cref="IMailStore.SendMessageAsync"/>; every other path leaves
    /// this empty.
    /// </summary>
    public IReadOnlyList<string> Unregistered { get; init; } = [];
}
