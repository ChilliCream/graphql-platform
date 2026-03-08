namespace Mocha.Sagas;

/// <summary>
/// An event indicating that a saga has timed out, allowing the saga to handle the timeout as a state transition.
/// </summary>
/// <param name="SagaId">The identifier of the saga that timed out.</param>
public sealed record SagaTimedOutEvent(Guid SagaId) : ICorrelatable
{
    /// <inheritdoc />
    public Guid? CorrelationId => SagaId;
}
