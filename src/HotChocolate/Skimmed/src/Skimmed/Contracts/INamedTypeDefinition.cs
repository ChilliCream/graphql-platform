using HotChocolate.Features;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL type definition of a named type.
/// </summary>
public interface INamedTypeDefinition
    : ITypeDefinition
    , INameProvider
    , IDescriptionProvider
    , IDirectivesProvider
    , IFeatureProvider
{
    /// <summary>
    /// Gets or sets the name of the type.
    /// </summary>
    /// <value>
    /// The name of the type.
    /// </value>
    new string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the type.
    /// </summary>
    /// <value>
    /// The description of the type.
    /// </value>
    new string? Description { get; set; }
}
