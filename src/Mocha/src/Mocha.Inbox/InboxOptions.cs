namespace Mocha.Inbox;

/// <summary>
/// Configuration options for the inbox.
/// </summary>
public sealed class InboxOptions
{
    /// <summary>
    /// Gets or sets the retention period for processed messages.
    /// Messages older than this will be cleaned up.
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Gets or sets the cleanup interval for removing old processed messages.
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
