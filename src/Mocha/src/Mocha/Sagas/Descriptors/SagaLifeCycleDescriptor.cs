using Mocha;

namespace Mocha.Sagas;

/// <summary>
/// Default implementation of <see cref="ISagaLifeCycleDescriptor{TState}"/> that builds a
/// lifecycle configuration for saga state entry actions.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
public sealed class SagaLifeCycleDescriptor<TState>
    : MessagingDescriptorBase<SagaLifeCycleConfiguration>
    , ISagaLifeCycleDescriptor<TState> where TState : SagaStateBase
{
    protected internal override SagaLifeCycleConfiguration Configuration { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaLifeCycleDescriptor{TState}"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    public SagaLifeCycleDescriptor(IMessagingConfigurationContext context) : base(context)
    {
        Configuration = new();
    }

    /// <inheritdoc />
    public ISagaLifeCycleDescriptor<TState> Publish<TMessage>(
        Func<IConsumeContext, TState, TMessage?> factory,
        SagaPublishOptions? sagaOptions)
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
    public ISagaLifeCycleDescriptor<TState> Send<TMessage>(
        Func<IConsumeContext, TState, TMessage?> factory,
        SagaSendOptions? sagaOptions)
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

    /// <summary>
    /// Creates the lifecycle configuration from the current descriptor state.
    /// </summary>
    /// <returns>The constructed lifecycle configuration.</returns>
    public SagaLifeCycleConfiguration CreateConfiguration()
    {
        return Configuration;
    }
}
