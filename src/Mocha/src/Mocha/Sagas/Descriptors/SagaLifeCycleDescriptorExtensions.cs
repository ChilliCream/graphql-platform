namespace Mocha.Sagas;

/// <summary>
/// Extension methods for <see cref="ISagaLifeCycleDescriptor{TState}"/> that provide convenience
/// methods for scheduling and simplified message dispatching in lifecycle actions.
/// </summary>
public static class SagaLifeCycleDescriptorExtensions
{
    /// <summary>
    /// Publishes a message with a scheduled delay as a lifecycle action.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="descriptor">The lifecycle descriptor to configure.</param>
    /// <param name="delay">The delay after which the message is published.</param>
    /// <param name="factory">A factory that creates the message from the saga state.</param>
    /// <returns>The lifecycle descriptor for chaining.</returns>
    public static ISagaLifeCycleDescriptor<TState> ScheduledPublish<TState, TMessage>(
        this ISagaLifeCycleDescriptor<TState> descriptor,
        TimeSpan delay,
        Func<TState, TMessage> factory)
        where TMessage : notnull
        where TState : SagaStateBase
    {
        // var options = new SagaPublishOptions
        // {
        //     ConfigureOptions = (_, _) => new PublishOptions { ScheduledTime = DateTimeOffset.UtcNow.Add(delay) }
        // };

        // return descriptor.Publish((_, state) => factory(state), options);

        // TODO for this we need scheduling
        throw new NotImplementedException(
            "Scheduled publish is not yet implemented. This requires support for delayed message dispatching in the underlying messaging system.");
    }

    /// <summary>
    /// Sends a request message with a scheduled delay as a lifecycle action.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <typeparam name="TMessage">The type of request message to send.</typeparam>
    /// <param name="descriptor">The lifecycle descriptor to configure.</param>
    /// <param name="delay">The delay after which the message is sent.</param>
    /// <param name="factory">A factory that creates the message from the saga state.</param>
    /// <returns>The lifecycle descriptor for chaining.</returns>
    public static ISagaLifeCycleDescriptor<TState> ScheduledSend<TState, TMessage>(
        this ISagaLifeCycleDescriptor<TState> descriptor,
        TimeSpan delay,
        Func<TState, TMessage> factory)
        where TMessage : notnull
        where TState : SagaStateBase
    {
        // var options = new SagaSendOptions
        // {
        //     ConfigureOptions = (_, _) => new SendOptions { ScheduledTime = DateTimeOffset.UtcNow.Add(delay) }
        // };

        // return descriptor.Send((_, state) => factory(state), options);
        // TODO for this we need scheduling
        throw new NotImplementedException(
            "Scheduled send is not yet implemented. This requires support for delayed message dispatching in the underlying messaging system.");
    }

    /// <summary>
    /// Publishes a message as a lifecycle action, using a simplified factory that only takes the saga state.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="descriptor">The lifecycle descriptor to configure.</param>
    /// <param name="factory">A factory that creates the message from the saga state.</param>
    /// <returns>The lifecycle descriptor for chaining.</returns>
    public static ISagaLifeCycleDescriptor<TState> Publish<TState, TMessage>(
        this ISagaLifeCycleDescriptor<TState> descriptor,
        Func<TState, TMessage> factory)
        where TMessage : notnull
        where TState : SagaStateBase
    {
        return descriptor.Publish((_, state) => factory(state), null);
    }

    /// <summary>
    /// Sends a request message as a lifecycle action, using a simplified factory that only takes the saga state.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <typeparam name="TMessage">The type of request message to send.</typeparam>
    /// <param name="descriptor">The lifecycle descriptor to configure.</param>
    /// <param name="factory">A factory that creates the message from the saga state.</param>
    /// <returns>The lifecycle descriptor for chaining.</returns>
    public static ISagaLifeCycleDescriptor<TState> Send<TState, TMessage>(
        this ISagaLifeCycleDescriptor<TState> descriptor,
        Func<TState, TMessage> factory)
        where TMessage : notnull
        where TState : SagaStateBase
    {
        return descriptor.Send((_, state) => factory(state), null);
    }
}
