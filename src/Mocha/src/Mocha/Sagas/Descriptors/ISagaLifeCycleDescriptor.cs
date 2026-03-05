using Mocha;

namespace Mocha.Sagas;

/// <summary>
/// Describes lifecycle actions for a saga state, such as publishing or sending messages on entry.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
public interface ISagaLifeCycleDescriptor<TState> : IMessagingDescriptor<SagaLifeCycleConfiguration>
    where TState : SagaStateBase
{
    /// <summary>
    /// Registers a message to be published when the lifecycle action triggers.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="factory">A factory that creates the message from the consume context and saga state, or <c>null</c> to skip.</param>
    /// <param name="sagaOptions">Optional publish options for the message.</param>
    /// <returns>This descriptor for chaining.</returns>
    ISagaLifeCycleDescriptor<TState> Publish<TMessage>(
        Func<IConsumeContext, TState, TMessage?> factory,
        SagaPublishOptions? sagaOptions)
        where TMessage : notnull;

    /// <summary>
    /// Registers a request message to be sent when the lifecycle action triggers.
    /// </summary>
    /// <typeparam name="TMessage">The type of request message to send.</typeparam>
    /// <param name="factory">A factory that creates the message from the consume context and saga state, or <c>null</c> to skip.</param>
    /// <param name="sagaOptions">Optional send options for the message.</param>
    /// <returns>This descriptor for chaining.</returns>
    ISagaLifeCycleDescriptor<TState> Send<TMessage>(
        Func<IConsumeContext, TState, TMessage?> factory,
        SagaSendOptions? sagaOptions)
        where TMessage : notnull;
}
