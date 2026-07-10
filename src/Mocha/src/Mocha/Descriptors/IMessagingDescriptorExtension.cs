namespace Mocha;

/// <summary>
/// Provides typed access to the underlying configuration of a descriptor for use by extensions.
/// </summary>
/// <typeparam name="T">The configuration type.</typeparam>
public interface IMessagingDescriptorExtension<out T> : IMessagingDescriptorExtension where T : MessagingConfiguration
{
    /// <summary>
    /// The type definition.
    /// </summary>
    new T Configuration { get; }

    MessagingConfiguration IMessagingDescriptorExtension.Configuration => Configuration;
}

/// <summary>
/// Provides untyped access to the underlying configuration and context of a descriptor for use by extensions.
/// </summary>
public interface IMessagingDescriptorExtension : IHasConfigurationContext
{
    /// <summary>
    /// The type definition.
    /// </summary>
    MessagingConfiguration Configuration { get; }
}
