namespace Mocha.Sagas;

/// <summary>
/// Default implementation of <see cref="ISagaDescriptor{TState}"/> that builds the saga configuration
/// from fluent descriptor calls.
/// </summary>
/// <typeparam name="TState">The saga state type.</typeparam>
public sealed class SagaDescriptor<TState> : MessagingDescriptorBase<SagaConfiguration>, ISagaDescriptor<TState>
    where TState : SagaStateBase
{
    private readonly List<SagaStateDescriptor<TState>> _states = [];

    private readonly SagaStateDescriptor<TState> _duringAny;

    protected internal override SagaConfiguration Configuration { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaDescriptor{TState}"/> class.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    public SagaDescriptor(IMessagingConfigurationContext context) : base(context)
    {
        Configuration = new();
        _duringAny = new SagaStateDescriptor<TState>(context, StateNames.DuringAny);
    }

    /// <summary>
    /// Sets the name of the saga.
    /// </summary>
    /// <param name="name">The saga name.</param>
    /// <returns>This descriptor for chaining.</returns>
    public ISagaDescriptor<TState> Name(string name)
    {
        Configuration.Name = name;

        return this;
    }

    /// <inheritdoc />
    public ISagaStateDescriptor<TState> Initially()
    {
        var descriptor = _states.SingleOrDefault(s => s.Configuration.IsInitial);

        if (descriptor is null)
        {
            descriptor = new SagaStateDescriptor<TState>(Context, StateNames.Initial);
            descriptor.Configuration.IsInitial = true;
            _states.Add(descriptor);
        }

        return descriptor;
    }

    /// <inheritdoc />
    public ISagaStateDescriptor<TState> During(string state)
    {
        var descriptor = _states.SingleOrDefault(s => s.Configuration.Name == state);

        if (descriptor is null)
        {
            descriptor = new SagaStateDescriptor<TState>(Context, state);
            _states.Add(descriptor);
        }

        return descriptor;
    }

    /// <inheritdoc />
    public ISagaStateDescriptor<TState> DuringAny()
    {
        return _duringAny;
    }

    /// <inheritdoc />
    public ISagaFinalStateDescriptor<TState> Finally(string state)
    {
        var descriptor = _states.SingleOrDefault(s => s.Configuration.Name == state);

        if (descriptor is null)
        {
            descriptor = new SagaStateDescriptor<TState>(Context, state);
            descriptor.Configuration.IsFinal = true;
            _states.Add(descriptor);
        }

        return descriptor;
    }

    /// <inheritdoc />
    public ISagaDescriptor<TState> Serializer(Func<IServiceProvider, ISagaStateSerializer> serializer)
    {
        Configuration.StateSerializer = serializer;

        return this;
    }

    /// <summary>
    /// Creates the saga configuration from the current descriptor state.
    /// </summary>
    /// <returns>The constructed saga configuration.</returns>
    public SagaConfiguration CreateConfiguration()
    {
        Configuration.States = _states.Select(s => s.CreateConfiguration()).ToList();
        Configuration.DuringAny = _duringAny.CreateConfiguration();

        return Configuration;
    }
}
