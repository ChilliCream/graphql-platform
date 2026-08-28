namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// The parameters for <see cref="IMailStore.QueryWorkspaceMessagesAsync"/>.
/// Every filter applies as AND with the others. Unlike
/// <see cref="MailInboxFilter"/> there is no <c>UnreadOnly</c> or
/// <c>IncludeArchived</c>: read and archived state live on
/// message_recipients per recipient and have no workspace-wide meaning.
/// </summary>
internal sealed record MailWorkspaceFilter
{
    /// <summary>
    /// Includes only messages this agent sent or received, as a to or cc
    /// recipient. Null returns every message in the workspace. Normalized
    /// via <see cref="MailAgentName.Normalize"/>.
    /// </summary>
    public string? Agent { get; init; }

    /// <summary>
    /// Includes only messages created at or after this instant.
    /// </summary>
    public DateTimeOffset? Since { get; init; }

    /// <summary>
    /// The maximum number of messages to return. Null means unlimited.
    /// </summary>
    public int? Limit { get; init; }
}
