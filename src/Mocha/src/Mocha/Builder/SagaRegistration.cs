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

    /// <summary>
    /// Optional pre-built state serializer provided by the source generator.
    /// When set, the saga uses this serializer instead of resolving one from the factory.
    /// </summary>
    public ISagaStateSerializer? StateSerializer { get; init; }
}
