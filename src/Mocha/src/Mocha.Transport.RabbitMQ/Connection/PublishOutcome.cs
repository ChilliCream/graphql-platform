namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Identifies how a publish ended.
/// </summary>
internal enum PublishOutcome
{
    /// <summary>
    /// The broker confirmed the message.
    /// </summary>
    Confirmed,

    /// <summary>
    /// The caller cancelled the publish.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The publish failed.
    /// </summary>
    Failed
}
