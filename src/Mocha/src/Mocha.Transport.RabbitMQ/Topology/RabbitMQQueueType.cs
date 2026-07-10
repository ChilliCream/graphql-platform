namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Provides constants for RabbitMQ queue types.
/// </summary>
public static class RabbitMQQueueType
{
    /// <summary>
    /// Classic queue type (default).
    /// </summary>
    public const string Classic = "classic";

    /// <summary>
    /// Quorum queue type for high availability and data safety.
    /// </summary>
    public const string Quorum = "quorum";

    /// <summary>
    /// Stream queue type for large-scale log streaming.
    /// </summary>
    public const string Stream = "stream";
}
