using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>Represents the scalar definition syntax.</para>
/// <para>
/// Scalar types represent primitive leaf values in a GraphQL type system.
/// GraphQL responses take the form of a hierarchical tree;
/// the leaves of this tree are typically GraphQL Scalar types
/// (but may also be Enum types or null values).
/// </para>
/// </summary>
public sealed class ScalarTypeDefinitionNode : NamedSyntaxNode, ITypeDefinitionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="ScalarTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the scalar.
    /// </param>
    /// <param name="description">
    /// The description of the scalar.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    public ScalarTypeDefinitionNode(
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives)
        : base(null, name, directives)
    {
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ScalarTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the scalar.
    /// </param>
    /// <param name="description">
    /// The description of the scalar.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    public ScalarTypeDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives)
        : base(location, name, directives)
    {
        Description = description;
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.ScalarTypeDefinition;

    /// <summary>
    /// Gets the scalar description.
    /// </summary>
    /// <value></value>
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
    public ScalarTypeDefinitionNode WithLocation(Location? location)
        => new(location, Name, Description, Directives);

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
    public ScalarTypeDefinitionNode WithName(NameNode name)
        => new(Location, name, Description, Directives);

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
    public ScalarTypeDefinitionNode WithDescription(StringValueNode? description)
        => new(Location, Name, description, Directives);

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
    public ScalarTypeDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Description, directives);
}
