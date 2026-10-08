namespace ChilliCream.Nitro.CommandLine.Services.Mail;

/// <summary>
/// Workspace mail query filters, combined with AND and independent of recipient
/// read or archive state.
/// </summary>
internal sealed record MailWorkspaceFilter
{
    /// <summary>
    /// Restricts results to mail sent or received by the normalized agent.
    /// Null or empty applies no agent filter.
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
