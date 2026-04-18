namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Default options for queues created by topology conventions.
/// </summary>
public sealed class AzureServiceBusDefaultQueueOptions
{
    /// <summary>
    /// Gets or sets whether queues are auto-deleted by default.
    /// Default is null (uses the queue default of false).
    /// </summary>
    public bool? AutoDelete { get; set; }

    /// <summary>
    /// Gets or sets whether queues are auto-provisioned by default.
    /// Default is null (uses the queue default of true).
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Default idle window after which the broker may delete the queue.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; set; }

    /// <summary>
    /// Default lock duration applied by the broker when a message is delivered to a receiver.
    /// </summary>
    public TimeSpan? LockDuration { get; set; }

    /// <summary>
    /// Default maximum delivery attempts before a message is dead-lettered.
    /// </summary>
    public int? MaxDeliveryCount { get; set; }

    /// <summary>
    /// Default time-to-live applied to messages that do not specify their own.
    /// </summary>
    public TimeSpan? DefaultMessageTimeToLive { get; set; }

    /// <summary>
    /// Default maximum queue size in megabytes.
    /// </summary>
    public long? MaxSizeInMegabytes { get; set; }

    /// <summary>
    /// Default value for whether queues require sessions.
    /// </summary>
    public bool? RequiresSession { get; set; }

    /// <summary>
    /// Default value for whether queues are partitioned.
    /// </summary>
    public bool? EnablePartitioning { get; set; }

    /// <summary>
    /// Default entity to which messages received on queues are auto-forwarded.
    /// </summary>
    public string? ForwardTo { get; set; }

    /// <summary>
    /// Default entity to which dead-lettered messages from queues are auto-forwarded.
    /// </summary>
    public string? ForwardDeadLetteredMessagesTo { get; set; }

    /// <summary>
    /// Default value for whether expired messages are moved to the dead-letter queue.
    /// </summary>
    public bool? DeadLetteringOnMessageExpiration { get; set; }

    /// <summary>
    /// Applies these defaults to a queue configuration, without overriding explicitly set values.
    /// </summary>
    internal void ApplyTo(AzureServiceBusQueueConfiguration configuration)
    {
        configuration.AutoProvision ??= AutoProvision;
        configuration.AutoDelete ??= AutoDelete;
        configuration.AutoDeleteOnIdle ??= AutoDeleteOnIdle;
        configuration.LockDuration ??= LockDuration;
        configuration.MaxDeliveryCount ??= MaxDeliveryCount;
        configuration.DefaultMessageTimeToLive ??= DefaultMessageTimeToLive;
        configuration.MaxSizeInMegabytes ??= MaxSizeInMegabytes;
        configuration.RequiresSession ??= RequiresSession;
        configuration.EnablePartitioning ??= EnablePartitioning;
        configuration.ForwardTo ??= ForwardTo;
        configuration.ForwardDeadLetteredMessagesTo ??= ForwardDeadLetteredMessagesTo;
        configuration.DeadLetteringOnMessageExpiration ??= DeadLetteringOnMessageExpiration;
    }
}
