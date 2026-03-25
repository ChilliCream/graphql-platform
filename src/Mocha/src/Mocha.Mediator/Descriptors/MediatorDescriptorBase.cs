namespace Mocha.Mediator;

/// <summary>
/// Base class for mediator descriptors that provides extension point access and the configuration context.
/// </summary>
/// <typeparam name="T">The configuration type this descriptor manages.</typeparam>
public abstract class MediatorDescriptorBase<T>(IMediatorConfigurationContext context)
    : IMediatorDescriptor<T>
    , IMediatorDescriptorExtension<T> where T : MediatorConfiguration
{
    protected internal IMediatorConfigurationContext Context { get; } =
        context ?? throw new ArgumentNullException(nameof(context));

    IMediatorConfigurationContext IHasMediatorConfigurationContext.Context => Context;

    protected internal abstract T Configuration { get; protected set; }

    T IMediatorDescriptorExtension<T>.Configuration => Configuration;

    public IMediatorDescriptorExtension<T> Extend() => this;

    IMediatorDescriptorExtension IMediatorDescriptor.Extend() => Extend();

    public IMediatorDescriptorExtension<T> ExtendWith(Action<IMediatorDescriptorExtension<T>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(this);
        return this;
    }

    public IMediatorDescriptorExtension<T> ExtendWith<TState>(Action<IMediatorDescriptorExtension<T>, TState> configure, TState state)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(this, state);
        return this;
    }
}
