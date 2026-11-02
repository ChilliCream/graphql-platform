namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Defines bus-level defaults that are applied to all auto-provisioned queues and exchanges
/// when they are created by topology conventions.
/// </summary>
public sealed class RabbitMQBusDefaults
{
    /// <summary>
    /// Gets or sets the default queue configuration that is applied to all auto-provisioned queues.
    /// Individual queue settings will override these defaults.
    /// </summary>
    public RabbitMQDefaultQueueOptions Queue { get; set; } = new();

    /// <summary>
    /// Gets or sets the default exchange configuration that is applied to all auto-provisioned exchanges.
    /// Individual exchange settings will override these defaults.
    /// </summary>
    public RabbitMQDefaultExchangeOptions Exchange { get; set; } = new();
}
