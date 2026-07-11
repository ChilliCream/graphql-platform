namespace Mocha.Sagas;

/// <summary>
/// Describes a final saga state that marks the saga as complete, optionally producing a response message.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
public interface ISagaFinalStateDescriptor<TState> : IMessagingDescriptor<SagaStateConfiguration>
    where TState : SagaStateBase
{
    /// <summary>
    /// Configures a response message to be produced when the saga reaches this final state.
    /// </summary>
    /// <typeparam name="TEvent">The response event type.</typeparam>
    /// <param name="reply">A factory that creates the response event from the current saga state.</param>
    /// <returns>This descriptor for chaining.</returns>
    ISagaFinalStateDescriptor<TState> Respond<TEvent>(Func<TState, TEvent> reply);

    /// <summary>
    /// Begins configuring lifecycle actions that execute when the saga enters this final state.
    /// </summary>
    /// <returns>A descriptor for configuring on-entry lifecycle actions.</returns>
    ISagaLifeCycleDescriptor<TState> OnEntry();
}
