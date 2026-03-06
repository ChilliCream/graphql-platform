namespace Mocha.Sagas;

/// <summary>
/// Describes the transitions and lifecycle actions for a saga state.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
public interface ISagaStateDescriptor<TState> : IMessagingDescriptor<SagaStateConfiguration>
    where TState : SagaStateBase
{
    /// <summary>
    /// Registers a transition triggered by a published event of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type that triggers the transition.</typeparam>
    /// <returns>A descriptor for configuring the transition behavior.</returns>
    ISagaTransitionDescriptor<TState, TEvent> OnEvent<TEvent>() where TEvent : notnull;

    /// <summary>
    /// Registers a transition triggered by a request message of the specified type.
    /// </summary>
    /// <typeparam name="TRequest">The request type that triggers the transition.</typeparam>
    /// <returns>A descriptor for configuring the transition behavior.</returns>
    ISagaTransitionDescriptor<TState, TRequest> OnRequest<TRequest>() where TRequest : notnull;

    /// <summary>
    /// Registers a transition triggered by a sent (point-to-point) message of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The event type that triggers the transition.</typeparam>
    /// <returns>A descriptor for configuring the transition behavior.</returns>
    ISagaTransitionDescriptor<TState, TEvent> OnSend<TEvent>() where TEvent : notnull;

    /// <summary>
    /// Registers a transition triggered by a reply message of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The reply event type that triggers the transition.</typeparam>
    /// <returns>A descriptor for configuring the transition behavior.</returns>
    ISagaTransitionDescriptor<TState, TEvent> OnReply<TEvent>() where TEvent : notnull;

    /// <summary>
    /// Begins configuring lifecycle actions that execute when the saga enters this state.
    /// </summary>
    /// <returns>A descriptor for configuring on-entry lifecycle actions.</returns>
    ISagaLifeCycleDescriptor<TState> OnEntry();
}
