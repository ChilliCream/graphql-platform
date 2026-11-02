namespace Mocha.Mediator;

/// <summary>
/// Provides typed access to the underlying configuration of a descriptor for use by extensions.
/// </summary>
/// <typeparam name="T">The configuration type.</typeparam>
public interface IMediatorDescriptorExtension<out T> : IMediatorDescriptorExtension where T : MediatorConfiguration
{
    /// <summary>
    /// The type definition.
    /// </summary>
    new T Configuration { get; }

    MediatorConfiguration IMediatorDescriptorExtension.Configuration => Configuration;
}

/// <summary>
/// Provides untyped access to the underlying configuration and context of a descriptor for use by extensions.
/// </summary>
public interface IMediatorDescriptorExtension : IHasMediatorConfigurationContext
{
    /// <summary>
    /// The type definition.
    /// </summary>
    MediatorConfiguration Configuration { get; }
}
