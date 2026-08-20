namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// One recipient of a message. Null <see cref="ReadAt"/> or
/// <see cref="ArchivedAt"/> mean the recipient has not yet read or archived
/// the message.
/// </summary>
internal sealed record MailRecipient
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the message_recipients table.
    /// </summary>
    public const string Columns =
        "recipient AS Name, kind AS Kind, ordinal AS Ordinal, "
        + "read_at AS ReadAt, archived_at AS ArchivedAt";

    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required int Ordinal { get; init; }
    public DateTimeOffset? ReadAt { get; init; }
    public DateTimeOffset? ArchivedAt { get; init; }
}
