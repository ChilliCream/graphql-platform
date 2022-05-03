using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

/// <summary>
/// The base class for input object types and input object type extensions.
/// </summary>
public abstract class InputObjectTypeDefinitionNodeBase : NamedSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="InputObjectTypeDefinitionNodeBase"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the input object type.
    /// </param>
    /// <param name="directives">
    /// The directives of the input object type.
    /// </param>
    /// <param name="fields">
    /// The input fields of the input object type.
    /// </param>
    protected InputObjectTypeDefinitionNodeBase(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<InputValueDefinitionNode> fields)
        : base(location, name, directives)
    {
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }

    /// <summary>
    /// Gets the input fields.
    /// </summary>
    public IReadOnlyList<InputValueDefinitionNode> Fields { get; }
}
