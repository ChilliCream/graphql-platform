namespace Mocha.Events;

/// <summary>
/// Thrown when a request-reply operation does not receive a response within the configured timeout.
/// </summary>
public sealed class ResponseTimeoutException(string correlationId, TimeSpan timeout)
    : Exception($"No response received for '{correlationId}' within {timeout.TotalSeconds}s.")
{
    /// <summary>
    /// Gets the correlation identifier of the timed-out request.
    /// </summary>
    public string CorrelationId => correlationId;

    /// <summary>
    /// Gets the timeout duration that was exceeded.
    /// </summary>
    public TimeSpan Timeout => timeout;
}
