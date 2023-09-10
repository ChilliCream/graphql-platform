using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// GraphQL Unions represent an object that could be one of a list of
/// GraphQL Object types, but provides for no guaranteed fields between
/// those types. They also differ from interfaces in that Object types
/// declare what interfaces they implement, but are not aware of what
/// unions contain them.
/// </summary>
public sealed class UnionTypeDefinitionNode : UnionTypeDefinitionNodeBase, ITypeDefinitionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="UnionTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the input object.
    /// </param>
    /// <param name="description">
    /// The description of the input object.
    /// </param>
    /// <param name="directives">
    /// The directives of this input object.
    /// </param>
    /// <param name="types">
    /// The types of the union type.
    /// </param>
    public UnionTypeDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> types)
        : base(location, name, directives, types)
    {
        Description = description;
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.UnionTypeDefinition;

    /// <summary>
    /// Gets the union type description.
    /// </summary>
    public StringValueNode? Description { get; }

    /// <inheritdoc />
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Description is { })
        {
            yield return Description;
        }

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
    public UnionTypeDefinitionNode WithLocation(Location? location)
        => new(location, Name, Description, Directives, Types);

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
    public UnionTypeDefinitionNode WithName(NameNode name)
        => new(Location, name, Description, Directives, Types);

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
    public UnionTypeDefinitionNode WithDescription(StringValueNode? description)
        => new(Location, Name, description, Directives, Types);

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
    public UnionTypeDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Description, directives, Types);

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
    public UnionTypeDefinitionNode WithTypes(IReadOnlyList<NamedTypeNode> types)
        => new(Location, Name, Description, Directives, types);
}
