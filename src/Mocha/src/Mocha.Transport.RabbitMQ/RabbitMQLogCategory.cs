namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Defines the logger categories of a RabbitMQ transport.
/// </summary>
internal static class RabbitMQLogCategory
{
    /// <summary>
    /// The logger category of the consumer connection, the receive endpoints and their deliveries.
    /// </summary>
    public const string Consumer = "Mocha.Transport.RabbitMQ.Consumer";

    /// <summary>
    /// The logger category of the dispatcher connection and of every send and publish.
    /// </summary>
    public const string Dispatcher = "Mocha.Transport.RabbitMQ.Dispatcher";
}
