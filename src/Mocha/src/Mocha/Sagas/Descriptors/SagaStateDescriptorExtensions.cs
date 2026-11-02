using Mocha.Events;

namespace Mocha.Sagas;

/// <summary>
/// Extension methods for <see cref="ISagaStateDescriptor{TState}"/> that provide convenience
/// methods for common transition types.
/// </summary>
public static class SagaStateDescriptorExtensions
{
    /// <summary>
    /// Registers a transition triggered by a fault notification for a message the saga sent or published.
    /// It does not cover failures of the saga's own transitions.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <param name="descriptor">The state descriptor to configure.</param>
    /// <returns>A descriptor for configuring the fault transition.</returns>
    public static ISagaTransitionDescriptor<TState, NotAcknowledgedEvent> OnFault<TState>(
        this ISagaStateDescriptor<TState> descriptor)
        where TState : SagaStateBase
    {
        return descriptor.OnReply<NotAcknowledgedEvent>();
    }

    /// <summary>
    /// Registers a transition triggered by any successful reply. Fault notifications are handled
    /// separately through <c>OnFault</c>.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <param name="descriptor">The state descriptor to configure.</param>
    /// <returns>A descriptor for configuring the reply transition.</returns>
    public static ISagaTransitionDescriptor<TState, object> OnAnyReply<TState>(
        this ISagaStateDescriptor<TState> descriptor)
        where TState : SagaStateBase
    {
        return descriptor.OnReply<object>();
    }

    /// <summary>
    /// Registers a transition triggered by a saga timeout event.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <param name="descriptor">The state descriptor to configure.</param>
    /// <returns>A descriptor for configuring the timeout transition.</returns>
    public static ISagaTransitionDescriptor<TState, SagaTimedOutEvent> OnTimeout<TState>(
        this ISagaStateDescriptor<TState> descriptor)
        where TState : SagaStateBase
    {
        return descriptor.OnRequest<SagaTimedOutEvent>();
    }
}
