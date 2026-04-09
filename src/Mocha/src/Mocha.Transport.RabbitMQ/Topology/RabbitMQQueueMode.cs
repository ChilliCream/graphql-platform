namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Specifies the mode of a RabbitMQ queue.
/// </summary>
public enum RabbitMQQueueMode
{
    /// <summary>
    /// Default mode - messages are kept in memory and optionally persisted to disk.
    /// </summary>
    Default,

    /// <summary>
    /// Lazy mode - messages are kept on disk and loaded into memory when needed.
    /// </summary>
    Lazy
}
