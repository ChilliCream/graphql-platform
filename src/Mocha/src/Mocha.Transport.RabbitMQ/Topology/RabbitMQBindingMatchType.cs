namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Specifies the match type for binding arguments in headers exchange.
/// </summary>
public enum RabbitMQBindingMatchType
{
    /// <summary>
    /// All header values must match (default).
    /// </summary>
    All,

    /// <summary>
    /// Any header value can match.
    /// </summary>
    Any
}
