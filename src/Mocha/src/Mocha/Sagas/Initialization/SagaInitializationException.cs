namespace Mocha.Sagas;

/// <summary>
/// An exception thrown when a saga fails to initialize, typically due to invalid state machine configuration.
/// </summary>
/// <param name="saga">The saga that failed to initialize.</param>
/// <param name="message">The error message describing the initialization failure.</param>
public sealed class SagaInitializationException(Saga saga, string message) : Exception(message)
{
    /// <summary>
    /// Gets the saga that failed to initialize.
    /// </summary>
    public Saga Saga { get; } = saga;
}
