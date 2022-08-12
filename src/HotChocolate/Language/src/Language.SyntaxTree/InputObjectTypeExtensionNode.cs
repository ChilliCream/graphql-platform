using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class InputObjectTypeExtensionNode
    : InputObjectTypeDefinitionNodeBase
    , ITypeExtensionNode
{
    public InputObjectTypeExtensionNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<InputValueDefinitionNode> fields)
        : base(location, name, directives, fields)
    {
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.InputObjectTypeExtension;

    /// <inheritdoc />
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;

        foreach (var directive in Directives)
        {
            yield return directive;
        }

        foreach (var field in Fields)
        {
            yield return field;
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
    public InputObjectTypeExtensionNode WithLocation(Location? location)
        => new(location, Name, Directives, Fields);

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
    public InputObjectTypeExtensionNode WithName(NameNode name)
        => new(Location, name, Directives, Fields);

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
    public InputObjectTypeExtensionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, directives, Fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="InputObjectTypeDefinitionNodeBase.Fields" /> with <paramref name="fields" />.
    /// </summary>
    /// <param name="fields">
    /// The fields that shall be used to replace the current
    /// <see cref="InputObjectTypeDefinitionNodeBase.Fields" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="fields" />.
    /// </returns>
    public InputObjectTypeExtensionNode WithFields(
        IReadOnlyList<InputValueDefinitionNode> fields)
        => new(Location, Name, Directives, fields);
}
