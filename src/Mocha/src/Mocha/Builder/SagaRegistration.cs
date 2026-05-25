using Mocha.Sagas;

namespace Mocha;

/// <summary>
/// Captures a deferred saga registration for deduplication.
/// </summary>
internal sealed class SagaRegistration
{
    /// <summary>
    /// The saga type that identifies this registration.
    /// </summary>
    public required Type SagaType { get; init; }

    /// <summary>
    /// Factory that creates the saga instance.
    /// </summary>
    public required Func<Saga> Factory { get; init; }
}
