using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

/// <summary>
/// The base class for union type and union type extension.
/// </summary>
public abstract class UnionTypeDefinitionNodeBase : NamedSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="UnionTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the input object.
    /// </param>
    /// <param name="directives">
    /// The directives of this input object.
    /// </param>
    /// <param name="types">
    /// The types of the union type.
    /// </param>
    protected UnionTypeDefinitionNodeBase(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> types)
        : base(location, name, directives)
    {
        Types = types ?? throw new ArgumentNullException(nameof(types));
    }

    /// <summary>
    /// Gets the types of the union type.
    /// </summary>
    public IReadOnlyList<NamedTypeNode> Types { get; }
}
