using Mocha;

namespace Mocha.Sagas;

/// <summary>
/// Configuration for lifecycle actions that execute when a saga enters a state.
/// </summary>
public sealed class SagaLifeCycleConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets the list of messages to publish when the lifecycle action triggers.
    /// </summary>
    public List<SagaEventPublishConfiguration> Publish { get; } = [];

    /// <summary>
    /// Gets the list of messages to send when the lifecycle action triggers.
    /// </summary>
    public List<SagaEventSendConfiguration> Send { get; } = [];
}
