using HotChocolate.Features;

namespace HotChocolate.Skimmed;

/// <summary>
/// The base interface for GraphQL field definitions.
/// </summary>
public interface IFieldDefinition
    : INameProvider
    , IDescriptionProvider
    , IDeprecationProvider
    , IDirectivesProvider
    , IFeatureProvider
{
    /// <summary>
    /// Gets or sets the
    /// </summary>
    /// <value></value>
    ITypeDefinition Type { get; set; }
}
