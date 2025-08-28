namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Represents a complex output type configuration.
/// </summary>
public interface IComplexOutputTypeConfiguration
{
    /// <summary>
    /// Gets the name of the type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the type.
    /// </summary>
    Type RuntimeType { get; }

    /// <summary>
    /// Gets a list of other known runtime types for this complex type.
    /// </summary>
    IList<Type> KnownRuntimeTypes { get; }

    /// <summary>
    /// Gets a list of interfaces that this complex type implements.
    /// </summary>
    IList<TypeReference> Interfaces { get; }
}
