namespace Mocha.Sagas;

/// <summary>
/// Default implementation of <see cref="ISagaStateDescriptor{TState}"/> and <see cref="ISagaFinalStateDescriptor{TState}"/>
/// that builds a saga state configuration from fluent descriptor calls.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
public sealed class SagaStateDescriptor<TState>
    : MessagingDescriptorBase<SagaStateConfiguration>
    , ISagaStateDescriptor<TState>
    , ISagaFinalStateDescriptor<TState> where TState : SagaStateBase
{
    private readonly SagaLifeCycleDescriptor<TState> _onEntry;
    private readonly List<SagaTransitionDescriptor<TState>> _transitions = [];

    protected internal override SagaStateConfiguration Configuration { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaStateDescriptor{TState}"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The name of the state being described.</param>
    public SagaStateDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new();
        Configuration.Name = name;
        _onEntry = new SagaLifeCycleDescriptor<TState>(context);
    }

    /// <inheritdoc />
    public ISagaTransitionDescriptor<TState, TEvent> OnEvent<TEvent>() where TEvent : notnull
    {
        var eventType = typeof(TEvent);

        if (eventType.IsEventRequest())
        {
            throw new InvalidOperationException(
                $"Event type '{eventType}' is a request and should be handled with 'OnRequest' method.");
        }

        return On<TEvent>(SagaTransitionKind.Event);
    }

    /// <inheritdoc />
    public ISagaTransitionDescriptor<TState, TRequest> OnRequest<TRequest>() where TRequest : notnull
    {
        return On<TRequest>(SagaTransitionKind.Request);
    }

    /// <inheritdoc />
    public ISagaTransitionDescriptor<TState, TEvent> OnSend<TEvent>() where TEvent : notnull
    {
        return On<TEvent>(SagaTransitionKind.Send);
    }

    /// <inheritdoc />
    public ISagaTransitionDescriptor<TState, TEvent> OnReply<TEvent>() where TEvent : notnull
    {
        var eventType = typeof(TEvent);

        if (eventType.IsEventRequest())
        {
            throw new InvalidOperationException(
                $"Event type '{eventType}' is a request and should be handled with 'OnRequest' method.");
        }

        return On<TEvent>(SagaTransitionKind.Reply);
    }

    private ISagaTransitionDescriptor<TState, TEvent> On<TEvent>(SagaTransitionKind transitionKind)
        where TEvent : notnull
    {
        var existing = _transitions.SingleOrDefault(t =>
            t.Configuration.EventType == typeof(TEvent) && t.Configuration.TransitionKind == transitionKind
        );

        if (existing is ISagaTransitionDescriptor<TState, TEvent> transitionDescriptor)
        {
            return transitionDescriptor;
        }

        var descriptor = new SagaTransitionDescriptor<TState, TEvent>(Context, transitionKind);
        _transitions.Add(descriptor);
        return descriptor;
    }

    /// <inheritdoc />
    public ISagaLifeCycleDescriptor<TState> OnEntry()
    {
        return _onEntry;
    }

    /// <inheritdoc />
    public ISagaFinalStateDescriptor<TState> Respond<TEvent>(Func<TState, TEvent> reply)
    {
        Configuration.Response = new() { EventType = typeof(TEvent), Factory = state => reply((TState)state)! };
        return this;
    }

    /// <summary>
    /// Creates the saga state configuration from the current descriptor state.
    /// </summary>
    /// <returns>The constructed saga state configuration.</returns>
    public SagaStateConfiguration CreateConfiguration()
    {
        Configuration.Transitions = [.. _transitions.Select(t => t.CreateConfiguration())];
        Configuration.OnEntry = _onEntry.CreateConfiguration();
        return Configuration;
    }

    // public static SagaStateDescriptor<TState> From(SagaStateConfiguration definition)
    // {
    //     return new SagaStateDescriptor<TState>(definition);
    // }
}
