using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

/// <summary>
/// The base class for enum type definitions.
/// </summary>
public abstract class EnumTypeDefinitionNodeBase : NamedSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="EnumTypeDefinitionNodeBase"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The enum type name.
    /// </param>
    /// <param name="directives">
    /// The directives applied to the enum type.
    /// </param>
    /// <param name="values">
    /// The enum values.
    /// </param>
    protected EnumTypeDefinitionNodeBase(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<EnumValueDefinitionNode> values)
        : base(location, name, directives)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }

    /// <summary>
    /// Gets the enum values.
    /// </summary>
    public IReadOnlyList<EnumValueDefinitionNode> Values { get; }
}
