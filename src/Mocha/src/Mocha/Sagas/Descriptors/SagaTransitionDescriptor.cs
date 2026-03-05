using Mocha;

namespace Mocha.Sagas;

/// <summary>
/// Base descriptor for saga transitions that builds a <see cref="SagaTransitionConfiguration"/>.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
public class SagaTransitionDescriptor<TState> : MessagingDescriptorBase<SagaTransitionConfiguration>
    where TState : SagaStateBase
{
    protected internal override SagaTransitionConfiguration Configuration { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaTransitionDescriptor{TState}"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    public SagaTransitionDescriptor(IMessagingConfigurationContext context) : base(context)
    {
        Configuration = new();
    }

    /// <summary>
    /// Creates the saga transition configuration from the current descriptor state.
    /// </summary>
    /// <returns>The constructed transition configuration.</returns>
    public SagaTransitionConfiguration CreateConfiguration()
    {
        return Configuration;
    }
}

/// <summary>
/// Typed implementation of <see cref="ISagaTransitionDescriptor{TState, TEvent}"/> that builds
/// a transition configuration for a specific event type.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
/// <typeparam name="TEvent">The event type that triggers the transition.</typeparam>
public sealed class SagaTransitionDescriptor<TState, TEvent>
    : SagaTransitionDescriptor<TState>
    , ISagaTransitionDescriptor<TState, TEvent> where TState : SagaStateBase where TEvent : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SagaTransitionDescriptor{TState, TEvent}"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="transitionKind">The kind of transition (event, send, request, or reply).</param>
    public SagaTransitionDescriptor(IMessagingConfigurationContext context, SagaTransitionKind transitionKind)
        : base(context)
    {
        Configuration.EventType = typeof(TEvent);
        Configuration.TransitionKind = transitionKind;
    }

    /// <inheritdoc />
    public ISagaTransitionDescriptor<TState, TEvent> Then(Action<TState, TEvent> action)
    {
        Configuration.Action = (state, evt) => action((TState)state, (TEvent)evt);

        return this;
    }

    /// <inheritdoc />
    public ISagaTransitionDescriptor<TState, TEvent> TransitionTo(string state)
    {
        Configuration.TransitionTo = state;

        return this;
    }

    /// <inheritdoc />
    public ISagaTransitionDescriptor<TState, TEvent> AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;

        return this;
    }

    /// <inheritdoc />
    public ISagaTransitionDescriptor<TState, TEvent> Publish<TMessage>(
        Func<IConsumeContext, TState, TMessage> factory,
        SagaPublishOptions? sagaOptions = null)
        where TMessage : notnull
    {
        Configuration.Publish.Add(
            new SagaEventPublishConfiguration
            {
                MessageType = typeof(TMessage),
                Factory = (context, state) => factory(context, (TState)state),
                Options = sagaOptions ?? SagaPublishOptions.Default
            });

        return this;
    }

    /// <inheritdoc />
    public ISagaTransitionDescriptor<TState, TEvent> Send<TMessage>(
        Func<IConsumeContext, TState, TMessage> factory,
        SagaSendOptions? sagaOptions = null)
        where TMessage : notnull
    {
        Configuration.Send.Add(
            new SagaEventSendConfiguration
            {
                MessageType = typeof(TMessage),
                Factory = (context, state) => factory(context, (TState)state),
                Options = sagaOptions ?? SagaSendOptions.Default
            });

        return this;
    }

    /// <inheritdoc />
    public ISagaTransitionDescriptor<TState, TEvent> StateFactory(Func<TEvent, TState> factory)
    {
        Configuration.StateFactory = @event => factory((TEvent)@event);

        return this;
    }
}
