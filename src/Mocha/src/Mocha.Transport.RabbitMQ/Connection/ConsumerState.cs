namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Identifies what a consumer is doing.
/// </summary>
internal enum ConsumerState
{
    /// <summary>
    /// The consumer waits for a ready connection.
    /// </summary>
    WaitingForConnection,

    /// <summary>
    /// The consumer opens a channel, declares its queue and starts consuming.
    /// </summary>
    Attaching,

    /// <summary>
    /// The consumer declared its queue and waits until a handler of a lost channel returns.
    /// </summary>
    WaitingForHandlers,

    /// <summary>
    /// The broker delivers messages to the consumer.
    /// </summary>
    Attached,

    /// <summary>
    /// The consumer was stopped.
    /// </summary>
    Stopped
}
