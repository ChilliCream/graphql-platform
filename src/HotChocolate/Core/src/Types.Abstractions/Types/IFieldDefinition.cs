namespace HotChocolate.Types;

/// <summary>
/// The base interface for GraphQL field definitions.
/// </summary>
public interface IFieldDefinition
    : INameProvider
    , IDescriptionProvider
    , IDeprecationProvider
    , IDirectivesProvider
    , ISyntaxNodeProvider
{
    /// <summary>
    /// Gets or sets the type of the field.
    /// </summary>
    /// <value></value>
    ITypeDefinition Type { get; }
}
