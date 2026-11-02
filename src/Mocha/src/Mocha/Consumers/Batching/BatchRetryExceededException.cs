namespace Mocha;

/// <summary>
/// Thrown when a message exceeds the maximum batch retry limit and should be
/// routed to the error endpoint instead of being re-batched.
/// </summary>
public sealed class BatchRetryExceededException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="BatchRetryExceededException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public BatchRetryExceededException(string message) : base(message) { }
}
