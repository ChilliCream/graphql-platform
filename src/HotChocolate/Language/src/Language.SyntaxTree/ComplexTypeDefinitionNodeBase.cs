using System;
using System.Collections.Generic;

namespace HotChocolate.Language;

/// <summary>
/// The base class for complex type definitions e.g. interface or object
/// </summary>
public abstract class ComplexTypeDefinitionNodeBase : NamedSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of
    /// <see cref="ComplexTypeDefinitionNodeBase"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name that this syntax node holds.
    /// </param>
    /// <param name="directives">
    /// The directives that are annotated to this syntax node.
    /// </param>
    /// <param name="interfaces">
    /// The interfaces that this type implements.
    /// </param>
    /// <param name="fields">
    /// The fields that this type exposes.
    /// </param>
    protected ComplexTypeDefinitionNodeBase(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> interfaces,
        IReadOnlyList<FieldDefinitionNode> fields)
        : base(location, name, directives)
    {
        Interfaces = interfaces ?? throw new ArgumentNullException(nameof(interfaces));
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }

    /// <summary>
    /// Gets the interfaces that this type implements.
    /// </summary>
    public IReadOnlyList<NamedTypeNode> Interfaces { get; }

    /// <summary>
    /// Gets the fields that this type exposes.
    /// </summary>
    public IReadOnlyList<FieldDefinitionNode> Fields { get; }
}
