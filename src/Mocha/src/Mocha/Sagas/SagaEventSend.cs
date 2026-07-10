namespace Mocha.Sagas;

/// <summary>
/// Defines an event to send during a saga state transition or lifecycle action.
/// </summary>
public sealed class SagaEventSend(
    Type messageType,
    Func<IConsumeContext, object, object?> factory,
    SagaSendOptions options)
{
    /// <summary>
    /// Gets the CLR type of the event to send.
    /// </summary>
    public Type MessageType { get; } = messageType;

    /// <summary>
    /// Gets the factory that creates the event from the consume context and saga state.
    /// </summary>
    public Func<IConsumeContext, object, object?> Factory { get; } = factory;

    /// <summary>
    /// Gets the send options for this event.
    /// </summary>
    public SagaSendOptions Options { get; } = options;
}
