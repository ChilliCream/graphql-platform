namespace HotChocolate.Types.Descriptors.Definitions;

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
