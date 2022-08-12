using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// GraphQL interfaces represent a list of named fields and their arguments.
/// GraphQL objects and interfaces can then implement these interfaces which requires
/// that the implementing type will define all fields defined by those interfaces.
///</para>
/// <para>
/// Fields on a GraphQL interface have the same rules as fields on a GraphQL object;
/// their type can be Scalar, Object, Enum, Interface, or Union, or any wrapping type
/// whose base type is one of those five.
/// </para>
/// <code>
/// interface NamedEntity {
///   name: String
/// }
/// </code>
/// </summary>
public sealed class InterfaceTypeDefinitionNode : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="InterfaceTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of this interface.
    /// </param>
    /// <param name="description">
    /// The description of this interface.
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
    public InterfaceTypeDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> interfaces,
        IReadOnlyList<FieldDefinitionNode> fields)
        : base(location, name, directives, interfaces, fields)
    {
        Description = description;
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.InterfaceTypeDefinition;

    /// <summary>
    /// Gets the interface description.
    /// </summary>
    public StringValueNode? Description { get; }

    /// <inheritdoc />
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Description is not null)
        {
            yield return Description;
        }

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
    public InterfaceTypeDefinitionNode WithLocation(Location? location)
        => new(location, Name, Description, Directives, Interfaces, Fields);

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
    public InterfaceTypeDefinitionNode WithName(NameNode name)
        => new(Location, name, Description, Directives, Interfaces, Fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Description" /> with <paramref name="description" />.
    /// </summary>
    /// <param name="description">
    /// The description that shall be used to replace the current description.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="description" />.
    /// </returns>
    public InterfaceTypeDefinitionNode WithDescription(StringValueNode? description)
        => new(Location, Name, description, Directives, Interfaces, Fields);

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
    public InterfaceTypeDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Description, directives, Interfaces, Fields);

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
    public InterfaceTypeDefinitionNode WithFields(IReadOnlyList<FieldDefinitionNode> fields)
        => new(Location, Name, Description, Directives, Interfaces, fields);

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
    public InterfaceTypeDefinitionNode WithInterfaces(
        IReadOnlyList<NamedTypeNode> interfaces)
        => new(Location, Name, Description, Directives, interfaces, Fields);
}
