using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// GraphQL operations are hierarchical and composed, describing a tree of information.
/// While Scalar types describe the leaf values of these hierarchical operations,
/// Objects describe the intermediate levels.
/// </para>
/// <para>
/// GraphQL Objects represent a list of named fields, each of which yield a value
/// of a specific type. Object values should be serialized as ordered maps,
/// where the selected field names (or aliases) are the keys and the result
/// of evaluating the field is the value, ordered by the order in which they
/// appear in the selection set.
/// </para>
/// <para>
/// All fields defined within an Object type must not have a name which begins with
/// "__" (two underscores), as this is used exclusively by GraphQL’s introspection system.
/// </para>
/// </summary>
public sealed class ObjectTypeDefinitionNode : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="ObjectTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name that this syntax node holds.
    /// </param>
    /// <param name="description">
    /// The description of the definition
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
    public ObjectTypeDefinitionNode(
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
    public override SyntaxKind Kind => SyntaxKind.ObjectTypeDefinition;

    /// <summary>
    /// Gets the description of this definition
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
    /// <see cref="Location"/> with <paramref name="location"/>.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location"/>
    /// </returns>
    public ObjectTypeDefinitionNode WithLocation(Location? location)
        => new(location, Name, Description, Directives, Interfaces, Fields);

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
    public ObjectTypeDefinitionNode WithName(NameNode name)
        => new(Location, name, Description, Directives, Interfaces, Fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="StringValueNode"/> with <paramref name="description"/>
    /// </summary>
    /// <param name="description">
    /// The description that shall be used to replace the current description.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="description"/>
    /// </returns>
    public ObjectTypeDefinitionNode WithDescription(StringValueNode? description)
        => new(Location, Name, description, Directives, Interfaces, Fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="IReadOnlyList&lt;DirectiveNode&gt;"/> with <paramref name="directives"/>
    /// </summary>
    /// <param name="directives">
    /// The directives that shall be used to replace the current directives.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="directives"/>
    /// </returns>
    public ObjectTypeDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Description, directives, Interfaces, Fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="IReadOnlyList&lt;NamedTypeNode&gt;"/> with <paramref name="interfaces"/>
    /// </summary>
    /// <param name="interfaces">
    /// The interfaces that shall be used to replace the current interfaces.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="interfaces"/>
    /// </returns>
    public ObjectTypeDefinitionNode WithInterfaces(IReadOnlyList<NamedTypeNode> interfaces)
        => new(Location, Name, Description, Directives, interfaces, Fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="IReadOnlyList&lt;FieldDefinitionNode&gt;"/> with <paramref name="fields"/>
    /// </summary>
    /// <param name="fields">
    /// The fields that shall be used to replace the current fields.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="fields"/>
    /// </returns>
    public ObjectTypeDefinitionNode WithFields(IReadOnlyList<FieldDefinitionNode> fields)
        => new(Location, Name, Description, Directives, Interfaces, fields);
}
