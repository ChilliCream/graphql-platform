using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// Interface type extensions are used to represent an interface which has been
/// extended from some original interface. For example, this might be used to
/// represent common local data on many types, or by a GraphQL service which is
/// itself an extension of another GraphQL service.
/// </para>
/// <code>
/// extend interface NamedEntity {
///   name: String
/// }
/// </code>
/// </summary>
public sealed class InterfaceTypeExtensionNode : ComplexTypeDefinitionNodeBase, ITypeExtensionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="InterfaceTypeExtensionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of this interface..
    /// </param>
    /// <param name="directives">
    /// The applied directives of this interface.
    /// </param>
    /// <param name="interfaces">
    /// The interfaces implemented by this interface.
    /// </param>
    /// <param name="fields">
    /// The fields of this interface.
    /// </param>
    public InterfaceTypeExtensionNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> interfaces,
        IReadOnlyList<FieldDefinitionNode> fields)
        : base(location, name, directives, interfaces, fields)
    {
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.InterfaceTypeExtension;

    /// <inheritdoc />
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;

        foreach (var interfaceName in Interfaces)
        {
            yield return interfaceName;
        }

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
    public InterfaceTypeExtensionNode WithLocation(Location? location)
        => new(location, Name, Directives, Interfaces, Fields);

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
    public InterfaceTypeExtensionNode WithName(NameNode name)
        => new(Location, name, Directives, Interfaces, Fields);

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
    public InterfaceTypeExtensionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, directives, Interfaces, Fields);

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
    public InterfaceTypeExtensionNode WithFields(IReadOnlyList<FieldDefinitionNode> fields)
        => new(Location, Name, Directives, Interfaces, fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="ComplexTypeDefinitionNodeBase.Interfaces" /> with <paramref name="interfaces" />.
    /// </summary>
    /// <param name="interfaces">
    /// The <paramref name="interfaces"/> that shall be used to replace the current
    /// <see cref="ComplexTypeDefinitionNodeBase.Fields" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="interfaces" />.
    /// </returns>
    public InterfaceTypeExtensionNode WithInterfaces(IReadOnlyList<NamedTypeNode> interfaces)
        => new(Location, Name, Directives, interfaces, Fields);
}
