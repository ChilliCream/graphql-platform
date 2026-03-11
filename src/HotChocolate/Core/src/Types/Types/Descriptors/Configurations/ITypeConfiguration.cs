#nullable disable

namespace HotChocolate.Types.Descriptors.Configurations;

/// <summary>
/// Represents a type definition.
/// </summary>
public interface ITypeConfiguration
    : ITypeSystemConfiguration
    , IRuntimeTypeProvider
    , IDirectiveConfigurationProvider
    , IExtendsTypeConfiguration
{
    /// <summary>
    /// Specifies that this type system object needs an explicit name completion since it
    /// depends on another type system object to complete its name.
    /// </summary>
    bool NeedsNameCompletion { get; set; }

    /// <summary>
    /// Gets or sets the runtime type.
    /// The runtime type defines of which value the type is when it
    /// manifests in the execution engine.
    /// </summary>
    new Type RuntimeType { get; set; }
}
