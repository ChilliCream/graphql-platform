namespace Mocha;

/// <summary>
/// Provides typed access to the underlying configuration of a descriptor for use by extensions.
/// </summary>
/// <typeparam name="T">The configuration type.</typeparam>
public interface IDescriptorExtension<out T> : IDescriptorExtension where T : MessagingConfiguration
{
    /// <summary>
    /// The type definition.
    /// </summary>
    new T Configuration { get; }

    MessagingConfiguration IDescriptorExtension.Configuration => Configuration;
}

/// <summary>
/// Provides untyped access to the underlying configuration and context of a descriptor for use by extensions.
/// </summary>
public interface IDescriptorExtension : IHasConfigurationContext
{
    /// <summary>
    /// The type definition.
    /// </summary>
    MessagingConfiguration Configuration { get; }
}
