namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Defines a factory for creating a type system configuration.
/// </summary>
public interface IConfigurationFactory
{
    /// <summary>
    /// Creates a new type system configuration.
    /// </summary>
    TypeSystemConfiguration CreateConfiguration();
}
