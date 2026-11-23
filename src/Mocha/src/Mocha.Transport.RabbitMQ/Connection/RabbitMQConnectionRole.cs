namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Defines the roles of the connections of a RabbitMQ transport, as they appear in connection names and logs.
/// </summary>
internal static class RabbitMQConnectionRole
{
    /// <summary>
    /// The role of the connection that consumes messages.
    /// </summary>
    public const string Consumer = "consumer";

    /// <summary>
    /// The role of the connection that publishes messages.
    /// </summary>
    public const string Dispatcher = "dispatcher";
}
