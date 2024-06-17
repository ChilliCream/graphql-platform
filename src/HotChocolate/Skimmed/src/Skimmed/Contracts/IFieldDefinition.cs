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
    /// Gets or sets the name of the field.
    /// </summary>
    /// <value>
    /// The name of the field.
    /// </value>
    new string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the field.
    /// </summary>
    /// <value>
    /// The description of the field.
    /// </value>
    new string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type of the field.
    /// </summary>
    /// <value></value>
    ITypeDefinition Type { get; set; }

    /// <summary>
    /// Defines if this <see cref="ITypeSystemMemberDefinition"/> is deprecated.
    /// </summary>
    new bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets the deprecation reason of this <see cref="ITypeSystemMemberDefinition"/>.
    /// </summary>
    new string? DeprecationReason { get; set; }
}
