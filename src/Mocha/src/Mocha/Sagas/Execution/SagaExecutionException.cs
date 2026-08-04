namespace Mocha.Sagas;

/// <summary>
/// An exception thrown when a saga encounters an error during execution.
/// </summary>
/// <param name="saga">The saga that encountered the error.</param>
/// <param name="message">The error message.</param>
public sealed class SagaExecutionException(Saga saga, string message) : Exception(message)
{
    /// <summary>
    /// Gets the saga that encountered the execution error.
    /// </summary>
    public Saga Saga { get; } = saga;
}
