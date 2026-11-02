namespace Mocha;

/// <summary>
/// Specifies the action the redelivery middleware should take for a failed message.
/// </summary>
internal enum RedeliveryAction
{
    /// <summary>
    /// Re-throw the exception; let outer middleware handle it.
    /// </summary>
    Rethrow,

    /// <summary>
    /// Discard the message; swallow the exception.
    /// </summary>
    Discard,

    /// <summary>
    /// Redeliver the message after a delay.
    /// </summary>
    Redeliver
}
