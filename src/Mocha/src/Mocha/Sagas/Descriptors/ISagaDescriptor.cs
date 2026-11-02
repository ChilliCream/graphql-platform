namespace Mocha.Sagas;

/// <summary>
/// Describes the configuration of a saga state machine, including its states, transitions, and serializer.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
public interface ISagaDescriptor<TState> : IMessagingDescriptor<SagaConfiguration> where TState : SagaStateBase
{
    /// <summary>
    /// Begins configuring the initial state of the saga, from which new saga instances start.
    /// </summary>
    /// <returns>A descriptor for configuring transitions on the initial state.</returns>
    ISagaStateDescriptor<TState> Initially();

    /// <summary>
    /// Begins configuring a named intermediate state that the saga can transition through.
    /// </summary>
    /// <param name="state">The name of the state to configure.</param>
    /// <returns>A descriptor for configuring transitions on the specified state.</returns>
    ISagaStateDescriptor<TState> During(string state);

    /// <summary>
    /// Begins configuring transitions that apply to all non-initial and non-final states.
    /// </summary>
    /// <returns>A descriptor for configuring transitions that apply universally.</returns>
    ISagaStateDescriptor<TState> DuringAny();

    /// <summary>
    /// Begins configuring a named final state that marks the saga as complete.
    /// </summary>
    /// <param name="state">The name of the final state to configure.</param>
    /// <returns>A descriptor for configuring the final state, including optional response generation.</returns>
    ISagaFinalStateDescriptor<TState> Finally(string state);

    /// <summary>
    /// Configures a custom serializer for the saga state.
    /// </summary>
    /// <param name="serializer">A factory that resolves the saga state serializer from the service provider.</param>
    /// <returns>This descriptor for chaining.</returns>
    ISagaDescriptor<TState> Serializer(Func<IServiceProvider, ISagaStateSerializer> serializer);
}
