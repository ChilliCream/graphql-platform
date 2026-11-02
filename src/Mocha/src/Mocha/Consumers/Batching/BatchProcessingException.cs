namespace Mocha;

/// <summary>
/// Thrown when a batch handler fails. Each per-message pipeline gets its own
/// instance to avoid shared exception mutation.
/// </summary>
internal sealed class BatchProcessingException : Exception
{
    public BatchProcessingException(string message, Exception innerException) : base(message, innerException) { }
}
