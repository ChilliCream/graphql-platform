namespace Mocha;

/// <summary>
/// Indicates why a batch was dispatched to the handler.
/// </summary>
public enum BatchCompletionMode
{
    /// <summary>
    /// Batch reached the configured maximum size.
    /// </summary>
    Size,

    /// <summary>
    /// Batch timeout expired with pending messages.
    /// </summary>
    Time,

    /// <summary>
    /// Endpoint is shutting down; remaining messages flushed.
    /// </summary>
    Forced
}
