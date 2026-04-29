namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Default options for topics created by topology conventions.
/// </summary>
public sealed class AzureServiceBusDefaultTopicOptions
{
    /// <summary>
    /// Gets or sets whether topics are auto-provisioned by default.
    /// Default is null (uses the topic default of true).
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Default time-to-live applied to messages that do not specify their own.
    /// </summary>
    public TimeSpan? DefaultMessageTimeToLive { get; set; }

    /// <summary>
    /// Default maximum topic size in megabytes.
    /// </summary>
    public long? MaxSizeInMegabytes { get; set; }

    /// <summary>
    /// Default value for whether topics are partitioned.
    /// </summary>
    public bool? EnablePartitioning { get; set; }

    /// <summary>
    /// Default value for whether topics enforce duplicate detection.
    /// </summary>
    public bool? RequiresDuplicateDetection { get; set; }

    /// <summary>
    /// Default time window over which duplicate detection is performed.
    /// </summary>
    public TimeSpan? DuplicateDetectionHistoryTimeWindow { get; set; }

    /// <summary>
    /// Default idle window after which the broker may delete the topic.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; set; }

    /// <summary>
    /// Default value for whether topics preserve ordering across partitioned subscriptions.
    /// </summary>
    public bool? SupportOrdering { get; set; }

    /// <summary>
    /// Applies these defaults to a topic configuration, without overriding explicitly set values.
    /// </summary>
    internal void ApplyTo(AzureServiceBusTopicConfiguration configuration)
    {
        configuration.AutoProvision ??= AutoProvision;
        configuration.DefaultMessageTimeToLive ??= DefaultMessageTimeToLive;
        configuration.MaxSizeInMegabytes ??= MaxSizeInMegabytes;
        configuration.EnablePartitioning ??= EnablePartitioning;
        configuration.RequiresDuplicateDetection ??= RequiresDuplicateDetection;
        configuration.DuplicateDetectionHistoryTimeWindow ??= DuplicateDetectionHistoryTimeWindow;
        configuration.AutoDeleteOnIdle ??= AutoDeleteOnIdle;
        configuration.SupportOrdering ??= SupportOrdering;
    }
}
