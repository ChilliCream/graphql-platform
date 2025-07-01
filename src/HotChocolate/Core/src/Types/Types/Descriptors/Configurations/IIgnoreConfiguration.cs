namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Represents definitions that carry an ignore flag.
/// </summary>
public interface IIgnoreConfiguration
{
    /// <summary>
    /// Defines if this field is ignored and will
    /// not be included into the schema.
    /// </summary>
    bool Ignore { get; }
}
