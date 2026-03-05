using Mocha;

namespace Mocha.Sagas;

/// <summary>
/// Defines an event to publish during a saga state transition or lifecycle action.
/// </summary>
public sealed class SagaEventPublish(
    Type messageType,
    Func<IConsumeContext, object, object?> factory,
    SagaPublishOptions options)
{
    /// <summary>
    /// Gets the CLR type of the event to publish.
    /// </summary>
    public Type MessageType { get; } = messageType;

    /// <summary>
    /// Gets the factory that creates the event from the consume context and saga state.
    /// </summary>
    public Func<IConsumeContext, object, object?> Factory { get; } = factory;

    /// <summary>
    /// Gets the publish options for this event.
    /// </summary>
    public SagaPublishOptions Options { get; } = options;
}
