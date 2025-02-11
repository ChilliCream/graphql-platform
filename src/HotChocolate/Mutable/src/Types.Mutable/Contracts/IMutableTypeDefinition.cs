using HotChocolate.Types.Mutable;

namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL type definition of a named type.
/// </summary>
public interface IMutableTypeDefinition : ITypeDefinition
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
    /// Gets the directives that are annotated to this type as mutable collection.
    /// </summary>
    new DirectiveCollection Directives { get; }
}
