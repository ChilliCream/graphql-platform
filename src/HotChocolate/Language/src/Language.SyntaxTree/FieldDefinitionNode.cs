using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents a filed definition of an interface- or object-type.
/// </summary>
public sealed class FieldDefinitionNode : NamedSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="FieldDefinitionNode"/>.
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
    /// <param name="arguments">
    /// The arguments of this field definition.
    /// </param>
    /// <param name="type">
    /// The return type of this field definition.
    /// </param>
    /// <param name="directives">
    /// The applied directives.
    /// </param>
    public FieldDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<InputValueDefinitionNode> arguments,
        ITypeNode type,
        IReadOnlyList<DirectiveNode> directives)
        : base(location, name, directives)
    {
        Description = description;
        Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.FieldDefinition;

    /// <summary>
    /// Gets the description of this field definition.
    /// </summary>
    public StringValueNode? Description { get; }

    /// <summary>
    /// Gets the arguments of this field definition.
    /// </summary>
    public IReadOnlyList<InputValueDefinitionNode> Arguments { get; }

    /// <summary>
    /// Gets the return type of this field definition.
    /// </summary>
    public ITypeNode Type { get; }

    /// <inheritdoc />
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Description is not null)
        {
            yield return Description;
        }

        yield return Name;

        foreach (var argument in Arguments)
        {
            yield return argument;
        }

        yield return Type;

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
    public FieldDefinitionNode WithLocation(Location? location)
        => new(location, Name, Description, Arguments, Type, Directives);

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
    public FieldDefinitionNode WithName(NameNode name)
        => new(Location, name, Description, Arguments, Type, Directives);

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
    public FieldDefinitionNode WithDescription(StringValueNode? description)
        => new(Location, Name, description, Arguments, Type, Directives);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Arguments" /> with <paramref name="arguments" />.
    /// </summary>
    /// <param name="arguments">
    /// The arguments that shall be used to replace the current <see cref="Arguments"/>.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="arguments" />.
    /// </returns>
    public FieldDefinitionNode WithArguments(IReadOnlyList<InputValueDefinitionNode> arguments)
        => new(Location, Name, Description, arguments, Type, Directives);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Type" /> with <paramref name="type" />.
    /// </summary>
    /// <param name="type">
    /// The type that shall be used to replace the current <see cref="Type"/>.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="type" />.
    /// </returns>
    public FieldDefinitionNode WithType(ITypeNode type)
        => new(Location, Name, Description, Arguments, type, Directives);

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
    public FieldDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Description, Arguments, Type, directives);
}
