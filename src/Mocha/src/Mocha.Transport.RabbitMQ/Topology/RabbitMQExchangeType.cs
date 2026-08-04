namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Provides constants for RabbitMQ exchange types.
/// </summary>
public static class RabbitMQExchangeType
{
    /// <summary>
    /// Direct exchange routes messages to queues based on exact routing key matches.
    /// Messages are delivered to queues whose binding key exactly matches the message routing key.
    /// </summary>
    public const string Direct = "direct";

    /// <summary>
    /// Fanout exchange broadcasts messages to all bound queues, ignoring routing keys.
    /// All queues bound to a fanout exchange receive a copy of every message published to it.
    /// </summary>
    public const string Fanout = "fanout";

    /// <summary>
    /// Topic exchange routes messages to queues based on pattern matching of routing keys.
    /// Supports wildcard patterns: * (single word) and # (zero or more words).
    /// </summary>
    public const string Topic = "topic";

    /// <summary>
    /// Headers exchange routes messages based on message header attributes instead of routing keys.
    /// Uses header attributes for routing decisions rather than routing key matching.
    /// </summary>
    public const string Headers = "headers";
}
