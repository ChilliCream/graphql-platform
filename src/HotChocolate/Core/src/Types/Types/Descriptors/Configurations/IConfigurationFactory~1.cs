namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Defines a factory for creating a type system configuration.
/// </summary>
/// <typeparam name="T">
/// The type of the type system configuration.
/// </typeparam>
public interface IConfigurationFactory<out T> : IConfigurationFactory where T : TypeSystemConfiguration
{
    /// <summary>
    /// Creates a new type system configuration.
    /// </summary>
    /// <returns></returns>
    new T CreateConfiguration();
}
