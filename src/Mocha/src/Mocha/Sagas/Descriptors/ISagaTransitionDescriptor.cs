namespace Mocha.Sagas;

/// <summary>
/// Describes a single transition within a saga state, including the action to perform, the target state,
/// and any messages to publish or send as side effects.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
/// <typeparam name="TEvent">The event type that triggers this transition.</typeparam>
public interface ISagaTransitionDescriptor<TState, TEvent> : IMessagingDescriptor<SagaTransitionConfiguration>
{
    /// <summary>
    /// Registers an action to execute on the saga state when the transition is triggered.
    /// </summary>
    /// <param name="action">The action that receives the current state and triggering event.</param>
    /// <returns>This descriptor for chaining.</returns>
    ISagaTransitionDescriptor<TState, TEvent> Then(Action<TState, TEvent> action);

    /// <summary>
    /// Specifies the target state to transition to after this transition completes.
    /// </summary>
    /// <param name="state">The name of the target state.</param>
    /// <returns>This descriptor for chaining.</returns>
    ISagaTransitionDescriptor<TState, TEvent> TransitionTo(string state);

    /// <summary>
    /// Controls whether the messaging infrastructure for this transition is automatically provisioned.
    /// </summary>
    /// <param name="autoProvision"><c>true</c> to automatically provision infrastructure; otherwise, <c>false</c>.</param>
    /// <returns>This descriptor for chaining.</returns>
    ISagaTransitionDescriptor<TState, TEvent> AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Registers a message to be published as a side effect of the transition.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="factory">A factory that creates the message from the consume context and saga state.</param>
    /// <param name="sagaOptions">Optional publish options for the message.</param>
    /// <returns>This descriptor for chaining.</returns>
    ISagaTransitionDescriptor<TState, TEvent> Publish<TMessage>(
        Func<IConsumeContext, TState, TMessage> factory,
        SagaPublishOptions? sagaOptions = null)
        where TMessage : notnull;

    /// <summary>
    /// Registers a message to be sent as a side effect of the transition.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to send.</typeparam>
    /// <param name="factory">A factory that creates the message from the consume context and saga state.</param>
    /// <param name="sagaOptions">Optional send options for the message.</param>
    /// <returns>This descriptor for chaining.</returns>
    ISagaTransitionDescriptor<TState, TEvent> Send<TMessage>(
        Func<IConsumeContext, TState, TMessage> factory,
        SagaSendOptions? sagaOptions = null)
        where TMessage : notnull;

    /// <summary>
    /// Registers a factory that creates new saga state instances when the transition triggers from the initial state.
    /// </summary>
    /// <param name="factory">A factory that creates a new saga state from the triggering event.</param>
    /// <returns>This descriptor for chaining.</returns>
    ISagaTransitionDescriptor<TState, TEvent> StateFactory(Func<TEvent, TState> factory);
}
