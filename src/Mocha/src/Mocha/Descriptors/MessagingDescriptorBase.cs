namespace Mocha;

/// <summary>
/// Base class for messaging descriptors that provides extension point access and the configuration context.
/// </summary>
/// <typeparam name="T">The configuration type this descriptor manages.</typeparam>
public abstract class MessagingDescriptorBase<T>(IMessagingConfigurationContext context)
    : IMessagingDescriptor<T>
    , IDescriptorExtension<T> where T : MessagingConfiguration
{
    protected internal IMessagingConfigurationContext Context { get; } =
        context ?? throw new ArgumentNullException(nameof(context));

    IMessagingConfigurationContext IHasConfigurationContext.Context => Context;

    protected internal abstract T Configuration { get; protected set; }

    T IDescriptorExtension<T>.Configuration => Configuration;

    public IDescriptorExtension<T> Extend() => this;

    IDescriptorExtension IMessagingDescriptor.Extend() => Extend();

    public IDescriptorExtension<T> ExtendWith(Action<IDescriptorExtension<T>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(this);
        return this;
    }

    public IDescriptorExtension<T> ExtendWith<TState>(Action<IDescriptorExtension<T>, TState> configure, TState state)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(this, state);
        return this;
    }
}
