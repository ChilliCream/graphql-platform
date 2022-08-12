using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// GraphQL Enum types, like Scalar types, also represent leaf values in a GraphQL type system.
/// However Enum types describe the set of possible values.
/// </para>
/// <para>
/// Enums are not references for a numeric value, but are unique values in their own right.
/// They may serialize as a string: the name of the represented value.
/// </para>
/// <para>
/// https://spec.graphql.org/October2021/#sec-Enums
/// </para>
/// </summary>
public sealed class EnumTypeDefinitionNode : EnumTypeDefinitionNodeBase, ITypeDefinitionNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="EnumTypeDefinitionNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name that this syntax node holds.
    /// </param>
    /// <param name="description">
    /// The description of the directive.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    /// <param name="values">
    /// The enum values.
    /// </param>
    public EnumTypeDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<EnumValueDefinitionNode> values)
        : base(location, name, directives, values)
    {
        Description = description;
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.EnumTypeDefinition;

    /// <summary>
    /// Gets the description of this enum type.
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
    public EnumTypeDefinitionNode WithLocation(Location? location)
        => new(location, Name, Description, Directives, Values);

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
    public EnumTypeDefinitionNode WithName(NameNode name)
        => new(Location, name, Description, Directives, Values);

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
    public EnumTypeDefinitionNode WithDescription(StringValueNode? description)
        => new(Location, Name, description, Directives, Values);

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
    public EnumTypeDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Description, directives, Values);

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
    public EnumTypeDefinitionNode WithValues(IReadOnlyList<EnumValueDefinitionNode> values)
        => new(Location, Name, Description, Directives, values);
}
