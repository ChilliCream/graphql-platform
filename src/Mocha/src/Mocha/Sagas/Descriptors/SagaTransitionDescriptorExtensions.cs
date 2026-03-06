namespace Mocha.Sagas;

/// <summary>
/// Extension methods for <see cref="ISagaTransitionDescriptor{TState, TEvent}"/> that provide convenience
/// methods for scheduling and simplified message dispatching.
/// </summary>
public static class SagaTransitionDescriptorExtensions
{
    /// <summary>
    /// Publishes a message with a scheduled delay as a side effect of the transition.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <typeparam name="TEvent">The triggering event type.</typeparam>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="descriptor">The transition descriptor to configure.</param>
    /// <param name="delay">The delay after which the message is published.</param>
    /// <param name="factory">A factory that creates the message from the saga state.</param>
    /// <returns>The transition descriptor for chaining.</returns>
    public static ISagaTransitionDescriptor<TState, TEvent> ScheduledPublish<TState, TEvent, TMessage>(
        this ISagaTransitionDescriptor<TState, TEvent> descriptor,
        TimeSpan delay,
        Func<TState, TMessage> factory)
        where TMessage : notnull
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
    /// Sends a request message with a scheduled delay as a side effect of the transition.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <typeparam name="TEvent">The triggering event type.</typeparam>
    /// <typeparam name="TMessage">The type of request message to send.</typeparam>
    /// <param name="descriptor">The transition descriptor to configure.</param>
    /// <param name="delay">The delay after which the message is sent.</param>
    /// <param name="factory">A factory that creates the message from the saga state.</param>
    /// <returns>The transition descriptor for chaining.</returns>
    public static ISagaTransitionDescriptor<TState, TEvent> ScheduledSend<TState, TEvent, TMessage>(
        this ISagaTransitionDescriptor<TState, TEvent> descriptor,
        TimeSpan delay,
        Func<TState, TMessage> factory)
        where TMessage : notnull
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
    /// Publishes a message as a side effect of the transition, using a simplified factory that only takes the saga state.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <typeparam name="TEvent">The triggering event type.</typeparam>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="descriptor">The transition descriptor to configure.</param>
    /// <param name="factory">A factory that creates the message from the saga state.</param>
    /// <returns>The transition descriptor for chaining.</returns>
    public static ISagaTransitionDescriptor<TState, TEvent> Publish<TState, TEvent, TMessage>(
        this ISagaTransitionDescriptor<TState, TEvent> descriptor,
        Func<TState, TMessage> factory)
        where TMessage : notnull
    {
        return descriptor.Publish((_, state) => factory(state));
    }

    /// <summary>
    /// Sends a message as a side effect of the transition, using a simplified factory that only takes the saga state.
    /// </summary>
    /// <typeparam name="TState">The saga state type.</typeparam>
    /// <typeparam name="TEvent">The triggering event type.</typeparam>
    /// <typeparam name="TMessage">The type of message to send.</typeparam>
    /// <param name="descriptor">The transition descriptor to configure.</param>
    /// <param name="factory">A factory that creates the message from the saga state.</param>
    /// <returns>The transition descriptor for chaining.</returns>
    public static ISagaTransitionDescriptor<TState, TEvent> Send<TState, TEvent, TMessage>(
        this ISagaTransitionDescriptor<TState, TEvent> descriptor,
        Func<TState, TMessage> factory)
        where TMessage : notnull
    {
        return descriptor.Send((_, state) => factory(state));
    }
}
