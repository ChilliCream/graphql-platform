using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Object type extensions are used to represent a type which has been extended
/// from some original type. For example, this might be used to represent local data,
/// or by a GraphQL service which is itself an extension of another GraphQL service.
/// </summary>
public sealed class ObjectTypeExtensionNode : ComplexTypeDefinitionNodeBase, ITypeExtensionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeExtensionNode"/>.
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
    public ObjectTypeExtensionNode(
        Location? location,
        NameNode name,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> interfaces,
        IReadOnlyList<FieldDefinitionNode> fields)
        : base(location, name, directives, interfaces, fields)
    {
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.ObjectTypeExtension;

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
    /// <see cref="Location"/> with <paramref name="location"/>.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location"/>
    /// </returns>
    public ObjectTypeExtensionNode WithLocation(Location? location)
        => new(location, Name, Directives, Interfaces, Fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NameNode"/> with <paramref name="name"/>
    /// </summary>
    /// <param name="name">
    /// The name that shall be used to replace the current name.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="name"/>
    /// </returns>
    public ObjectTypeExtensionNode WithName(NameNode name)
        => new(Location, name, Directives, Interfaces, Fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="NamedSyntaxNode.Directives"/> with <paramref name="directives"/>
    /// </summary>
    /// <param name="directives">
    /// The directives that shall be used to replace the current directives.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="directives"/>
    /// </returns>
    public ObjectTypeExtensionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, directives, Interfaces, Fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="ComplexTypeDefinitionNodeBase.Interfaces"/> with <paramref name="interfaces"/>
    /// </summary>
    /// <param name="interfaces">
    /// The interfaces that shall be used to replace the current interfaces.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="interfaces"/>
    /// </returns>
    public ObjectTypeExtensionNode WithInterfaces(IReadOnlyList<NamedTypeNode> interfaces)
        => new(Location, Name, Directives, interfaces, Fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="ComplexTypeDefinitionNodeBase.Fields"/> with <paramref name="fields"/>
    /// </summary>
    /// <param name="fields">
    /// The fields that shall be used to replace the current fields.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="fields"/>
    /// </returns>
    public ObjectTypeExtensionNode WithFields(IReadOnlyList<FieldDefinitionNode> fields)
        => new(Location, Name, Directives, Interfaces, fields);
}
