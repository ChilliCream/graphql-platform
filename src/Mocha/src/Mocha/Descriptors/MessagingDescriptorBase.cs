namespace Mocha;

/// <summary>
/// Base class for messaging descriptors that provides extension point access and the configuration context.
/// </summary>
/// <typeparam name="T">The configuration type this descriptor manages.</typeparam>
public abstract class MessagingDescriptorBase<T>(IMessagingConfigurationContext context)
    : IMessagingDescriptor<T>
    , IMessagingDescriptorExtension<T> where T : MessagingConfiguration
{
    protected internal IMessagingConfigurationContext Context { get; } =
        context ?? throw new ArgumentNullException(nameof(context));

    IMessagingConfigurationContext IHasConfigurationContext.Context => Context;

    protected internal abstract T Configuration { get; protected set; }

    T IMessagingDescriptorExtension<T>.Configuration => Configuration;

    public IMessagingDescriptorExtension<T> Extend() => this;

    IMessagingDescriptorExtension IMessagingDescriptor.Extend() => Extend();

    public IMessagingDescriptorExtension<T> ExtendWith(Action<IMessagingDescriptorExtension<T>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(this);
        return this;
    }

    public IMessagingDescriptorExtension<T> ExtendWith<TState>(Action<IMessagingDescriptorExtension<T>, TState> configure, TState state)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(this, state);
        return this;
    }
}
