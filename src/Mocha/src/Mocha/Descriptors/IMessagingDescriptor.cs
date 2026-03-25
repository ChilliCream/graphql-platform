namespace Mocha;

/// <summary>
/// A typed messaging descriptor that provides access to extension points for the underlying configuration.
/// </summary>
/// <typeparam name="T">The configuration type managed by this descriptor.</typeparam>
public interface IMessagingDescriptor<out T> : IMessagingDescriptor where T : MessagingConfiguration
{
    /// <summary>
    /// Provides access to the underlying configuration. This is useful for extensions.
    /// </summary>
    new IMessagingDescriptorExtension<T> Extend();

    /// <summary>
    /// Provides access to the underlying configuration. This is useful for extensions.
    /// </summary>
    IMessagingDescriptorExtension<T> ExtendWith(Action<IMessagingDescriptorExtension<T>> configure);

    /// <summary>
    /// Provides access to the underlying configuration. This is useful for extensions.
    /// </summary>
    IMessagingDescriptorExtension<T> ExtendWith<TState>(Action<IMessagingDescriptorExtension<T>, TState> configure, TState state);
}

/// <summary>
/// An untyped messaging descriptor that provides access to extension points for the underlying configuration.
/// </summary>
public interface IMessagingDescriptor
{
    /// <summary>
    /// Provides access to the underlying configuration. This is useful for extensions.
    /// </summary>
    IMessagingDescriptorExtension Extend();
}
