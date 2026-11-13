namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Identifies whether a failed RabbitMQ operation can succeed on a later attempt.
/// </summary>
internal enum RabbitMQFailureKind
{
    /// <summary>
    /// The connection or the broker is unavailable, or the broker rejected a message it can accept later, and
    /// a later attempt can succeed.
    /// </summary>
    Transient,

    /// <summary>
    /// The broker or the configuration refuses the operation, and it fails the same way on every attempt.
    /// </summary>
    Permanent
}
