using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Enum type extensions are used to represent an enum type which has been extended
/// from some original enum type. For example, this might be used to represent additional
/// local data, or by a GraphQL service which is itself an extension of another GraphQL service.
/// https://spec.graphql.org/October2021/#sec-Enum-Extensions
/// </summary>
public sealed class EnumTypeExtensionNode : EnumTypeDefinitionNodeBase, ITypeExtensionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeExtensionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name that this syntax node holds.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    /// <param name="values">
    /// The enum values.
    /// </param>
    public EnumTypeExtensionNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<EnumValueDefinitionNode> values)
        : base(location, name, directives, values)
    {
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.EnumTypeExtension;

    /// <inheritdoc />
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;

        foreach (var directive in Directives)
        {
            yield return directive;
        }

        foreach (var value in Values)
        {
            yield return value;
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
    public EnumTypeExtensionNode WithLocation(Location? location)
        => new(location, Name, Directives, Values);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NamedSyntaxNode.Name" /> with <paramref name="name" />.
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current name.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name" />.
    /// </returns>
    public EnumTypeExtensionNode WithName(NameNode name)
        => new(Location, name, Directives, Values);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NamedSyntaxNode.Directives" /> with <paramref name="directives" />.
    /// </summary>
    /// <param name="directives">
    /// The directives that shall be used to replace the current directives.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="directives" />.
    /// </returns>
    public EnumTypeExtensionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, directives, Values);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="EnumTypeDefinitionNodeBase.Values" /> with <paramref name="values" />.
    /// </summary>
    /// <param name="values">
    /// The values that shall be used to replace the current values.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="values" />.
    /// </returns>
    public EnumTypeExtensionNode WithValues(IReadOnlyList<EnumValueDefinitionNode> values)
        => new(Location, Name, Directives, values);
}
