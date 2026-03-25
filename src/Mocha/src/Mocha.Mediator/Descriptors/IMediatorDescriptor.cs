namespace Mocha.Mediator;

/// <summary>
/// A typed mediator descriptor that provides access to extension points for the underlying configuration.
/// </summary>
/// <typeparam name="T">The configuration type managed by this descriptor.</typeparam>
public interface IMediatorDescriptor<out T> : IMediatorDescriptor where T : MediatorConfiguration
{
    /// <summary>
    /// Provides access to the underlying configuration. This is useful for extensions.
    /// </summary>
    new IMediatorDescriptorExtension<T> Extend();

    /// <summary>
    /// Provides access to the underlying configuration. This is useful for extensions.
    /// </summary>
    IMediatorDescriptorExtension<T> ExtendWith(Action<IMediatorDescriptorExtension<T>> configure);

    /// <summary>
    /// Provides access to the underlying configuration. This is useful for extensions.
    /// </summary>
    IMediatorDescriptorExtension<T> ExtendWith<TState>(Action<IMediatorDescriptorExtension<T>, TState> configure, TState state);
}

/// <summary>
/// An untyped mediator descriptor that provides access to extension points for the underlying configuration.
/// </summary>
public interface IMediatorDescriptor
{
    /// <summary>
    /// Provides access to the underlying configuration. This is useful for extensions.
    /// </summary>
    IMediatorDescriptorExtension Extend();
}
