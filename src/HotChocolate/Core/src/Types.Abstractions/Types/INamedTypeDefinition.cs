namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL type definition of a named type.
/// </summary>
public interface INamedTypeDefinition
    : ITypeDefinition
    , INameProvider
    , IDescriptionProvider
    , IDirectivesProvider
    , ISyntaxNodeProvider
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

    /// <summary>
    /// Determines whether an instance of a specified type <paramref name="type" />
    /// can be assigned to a variable of the current type.
    /// </summary>
    bool IsAssignableFrom(INamedTypeDefinition type);
}
