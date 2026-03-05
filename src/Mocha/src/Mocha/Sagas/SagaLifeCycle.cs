using System.Collections.Immutable;

namespace Mocha.Sagas;

/// <summary>
/// Defines the lifecycle actions (publish and send operations) that are executed when a saga enters a state.
/// </summary>
public sealed class SagaLifeCycle(IEnumerable<SagaEventPublish> publish, IEnumerable<SagaEventSend> send)
{
    /// <summary>
    /// Gets the events to publish when entering this state.
    /// </summary>
    public ImmutableArray<SagaEventPublish> Publish { get; } = [.. publish];

    /// <summary>
    /// Gets the events to send when entering this state.
    /// </summary>
    public ImmutableArray<SagaEventSend> Send { get; } = [.. send];
}
