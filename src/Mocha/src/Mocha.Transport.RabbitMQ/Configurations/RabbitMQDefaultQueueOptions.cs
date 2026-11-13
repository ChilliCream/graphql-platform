namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Default options for queues created by topology conventions.
/// </summary>
public sealed class RabbitMQDefaultQueueOptions
{
    /// <summary>
    /// Gets or sets the default queue type (Classic, Quorum, or Stream).
    /// When no queue type is configured, RabbitMQ selects the queue type.
    /// </summary>
    public string? QueueType { get; set; }

    /// <summary>
    /// Gets or sets whether queues are durable by default.
    /// Default is null (uses the RabbitMQ default of true).
    /// </summary>
    public bool? Durable { get; set; }

    /// <summary>
    /// Gets or sets whether queues are auto-deleted by default.
    /// Default is null (uses the RabbitMQ default of false).
    /// </summary>
    public bool? AutoDelete { get; set; }

    /// <summary>
    /// Gets or sets additional default arguments applied to all auto-provisioned queues.
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; } = new();

    /// <summary>
    /// Applies queue defaults without overriding explicitly configured values.
    /// Auto-delete and exclusive queues skip queue type and argument defaults; non-durable queues skip them
    /// when the defaults specify quorum or stream.
    /// </summary>
    internal void ApplyTo(RabbitMQQueueConfiguration configuration)
    {
        configuration.Durable ??= Durable;
        configuration.AutoDelete ??= AutoDelete;

        var queueType = GetQueueType();

        var isIncompatibleWithQueueType =
            configuration.AutoDelete is true
            || configuration.Exclusive is true
            || (configuration.Durable is false
                && queueType is RabbitMQQueueType.Quorum or RabbitMQQueueType.Stream);

        if (isIncompatibleWithQueueType)
        {
            return;
        }

        if (Arguments.Count > 0 || QueueType is not null)
        {
            configuration.Arguments ??= new Dictionary<string, object>();

            if (QueueType is not null)
            {
                configuration.Arguments.TryAdd("x-queue-type", QueueType);
            }

            foreach (var (key, value) in Arguments)
            {
                configuration.Arguments.TryAdd(key, value);
            }
        }
    }

    /// <summary>
    /// Applies quorum replication settings to a generated reply queue.
    /// Message limits and queue lifecycle defaults are not applied.
    /// </summary>
    internal void ApplyToReplyQueue(RabbitMQQueueConfiguration configuration)
    {
        if (GetQueueType() is not RabbitMQQueueType.Quorum)
        {
            return;
        }

        configuration.Arguments ??= new Dictionary<string, object>();
        configuration.Arguments.TryAdd("x-queue-type", RabbitMQQueueType.Quorum);
        if (Arguments.TryGetValue("x-quorum-initial-group-size", out var groupSize))
        {
            configuration.Arguments.TryAdd("x-quorum-initial-group-size", groupSize);
        }
    }

    private string? GetQueueType()
        => QueueType ?? (Arguments.TryGetValue("x-queue-type", out var value) ? value as string : null);
}
