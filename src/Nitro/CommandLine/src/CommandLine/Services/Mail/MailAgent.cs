namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// A registered mail agent.
/// </summary>
internal sealed record MailAgent
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the agents table.
    /// </summary>
    public const string Columns =
        "name AS Name, registered_at AS RegisteredAt, last_seen_at AS LastSeenAt";

    public required string Name { get; init; }
    public required DateTimeOffset RegisteredAt { get; init; }
    public required DateTimeOffset LastSeenAt { get; init; }
}
