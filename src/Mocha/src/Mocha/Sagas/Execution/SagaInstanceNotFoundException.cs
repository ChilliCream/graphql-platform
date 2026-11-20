namespace Mocha.Sagas;

/// <summary>
/// An exception thrown when an event correlated through <see cref="ICorrelatable"/> arrives for a saga
/// instance that does not exist.
/// </summary>
/// <param name="saga">The saga that received the event.</param>
/// <param name="correlationId">The correlation identifier of the missing saga instance.</param>
/// <param name="message">The error message.</param>
public sealed class SagaInstanceNotFoundException(Saga saga, Guid correlationId, string message)
    : Exception(message)
{
    /// <summary>
    /// Gets the saga that received the event.
    /// </summary>
    public Saga Saga { get; } = saga;

    /// <summary>
    /// Gets the correlation identifier of the missing saga instance.
    /// </summary>
    public Guid CorrelationId { get; } = correlationId;
}
