using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// A GraphQL Input Object defines a set of input fields; the input fields are either scalars, enums, or other input objects. This allows arguments to accept arbitrarily complex structs.
/// </para>
/// <para>
/// In this example, an Input Object called Point2D describes x and y inputs:
/// <code>
/// input Point2D {
///   x: Float
///   y: Float
/// }
/// </code>
/// </para>
/// </summary>
public sealed class InputObjectTypeDefinitionNode
    : InputObjectTypeDefinitionNodeBase
    , ITypeDefinitionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="InputObjectTypeDefinitionNode"/>.
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
    /// <param name="fields">
    /// The fields of this input object.
    /// </param>
    public InputObjectTypeDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<InputValueDefinitionNode> fields)
        : base(location, name, directives, fields)
    {
        Description = description;
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.InputObjectTypeDefinition;

    /// <summary>
    /// Gets the input object description.
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
    public InputObjectTypeDefinitionNode WithLocation(Location? location)
        => new(location, Name, Description, Directives, Fields);

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
    public InputObjectTypeDefinitionNode WithName(NameNode name)
        => new(Location, name, Description, Directives, Fields);

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
    public InputObjectTypeDefinitionNode WithDescription(
        StringValueNode? description)
        => new(Location, Name, description, Directives, Fields);

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
    public InputObjectTypeDefinitionNode WithDirectives(
        IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Description, directives, Fields);

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
    public InputObjectTypeDefinitionNode WithFields(
        IReadOnlyList<InputValueDefinitionNode> fields)
        => new(Location, Name, Description, Directives, fields);
}
