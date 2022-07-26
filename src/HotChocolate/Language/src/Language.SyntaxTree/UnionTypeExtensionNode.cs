using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Union type extensions are used to represent a union type which has been
/// extended from some original union type.
/// </summary>
public sealed class UnionTypeExtensionNode : UnionTypeDefinitionNodeBase, ITypeExtensionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="UnionTypeExtensionNode"/>.
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
    public UnionTypeExtensionNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> types)
        : base(location, name, directives, types)
    {
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.UnionTypeExtension;

    /// <inheritdoc />
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;

        foreach (var directive in Directives)
        {
            yield return directive;
        }

        foreach (var type in Types)
        {
            yield return type;
        }
    }

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString() => SyntaxPrinter.Print(this, true);

    /// <summary>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </summary>
    /// <param name="indented">
    /// A value that indicates whether the GraphQL output should be formatted,
    /// which includes indenting nested GraphQL tokens, adding
    /// new lines, and adding white space between property names and values.
    /// </param>
    /// <returns>
    /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
    /// </returns>
    public override string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Location" /> with <paramref name="location" />.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location" />.
    /// </returns>
    public UnionTypeExtensionNode WithLocation(Location? location)
        => new(location, Name, Directives, Types);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NamedSyntaxNode.Name" /> with <paramref name="name" />.
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current <see cref="NamedSyntaxNode.Name" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name" />.
    /// </returns>
    public UnionTypeExtensionNode WithName(NameNode name)
        => new(Location, name, Directives, Types);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NamedSyntaxNode.Directives" /> with <paramref name="directives" />.
    /// </summary>
    /// <param name="directives">
    /// The directives that shall be used to replace the current
    /// <see cref="NamedSyntaxNode.Directives" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="directives" />.
    /// </returns>
    public UnionTypeExtensionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, directives, Types);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Types" /> with <paramref name="types" />.
    /// </summary>
    /// <param name="types">
    /// The types that shall be used to replace the current
    /// <see cref="Types" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="types" />.
    /// </returns>
    public UnionTypeExtensionNode WithTypes(IReadOnlyList<NamedTypeNode> types)
        => new(Location, Name, Directives, types);
}
