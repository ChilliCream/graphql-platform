namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// The parameters for <see cref="IMailStore.QueryInboxAsync"/>. Every filter
/// applies as AND with the others.
/// </summary>
internal sealed record MailInboxFilter
{
    /// <summary>
    /// The agent whose inbox is queried. Only messages addressing this agent
    /// as a to or cc recipient match.
    /// </summary>
    public required string Actor { get; init; }

    /// <summary>
    /// Includes only messages the actor has not yet read.
    /// </summary>
    public bool UnreadOnly { get; init; }

    /// <summary>
    /// Includes only messages sent by this agent.
    /// </summary>
    public string? From { get; init; }

    /// <summary>
    /// Includes only messages created at or after this instant.
    /// </summary>
    public DateTimeOffset? Since { get; init; }

    /// <summary>
    /// When false, excludes messages the actor has archived.
    /// </summary>
    public bool IncludeArchived { get; init; }

    /// <summary>
    /// The maximum number of messages to return. Null means unlimited.
    /// </summary>
    public int? Limit { get; init; }
}
