namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Default options for queues created by topology conventions.
/// </summary>
public sealed class RabbitMQDefaultQueueOptions
{
    /// <summary>
    /// Gets or sets the default queue type (Classic, Quorum, or Stream).
    /// When set, all auto-provisioned queues will use this queue type unless explicitly overridden.
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
    /// Applies these defaults to a queue configuration, without overriding explicitly set values.
    /// Quorum and stream queue types are not applied to queues that have auto-delete or exclusive
    /// set, since those properties are incompatible with these queue types. Default arguments are
    /// also skipped for incompatible queues because they may contain queue-type-specific settings
    /// (e.g. <c>x-delivery-limit</c> is only valid for quorum queues).
    /// </summary>
    internal void ApplyTo(RabbitMQQueueConfiguration configuration)
    {
        configuration.Durable ??= Durable;
        configuration.AutoDelete ??= AutoDelete;

        // Quorum and stream queues do not support auto-delete or exclusive properties.
        // Skip applying queue type and default arguments for incompatible configurations
        // (e.g. reply queues) since default arguments are often queue-type-specific.
        var isIncompatibleWithQueueType =
            configuration.AutoDelete is true || configuration.Exclusive is true;

        if (isIncompatibleWithQueueType)
        {
            return;
        }

        if (Arguments.Count > 0 || QueueType is not null)
        {
            configuration.Arguments ??= new Dictionary<string, object>();
            configuration.Arguments.TryAdd("x-queue-type", QueueType);

            foreach (var (key, value) in Arguments)
            {
                configuration.Arguments.TryAdd(key, value);
            }
        }
    }
}
