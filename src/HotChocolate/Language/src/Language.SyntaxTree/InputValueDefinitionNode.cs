using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// Represents the field definition of an input object.
/// </para>
/// <para>
/// A GraphQL Input Object defines a set of input fields; the input fields are either
/// scalars, enums, or other input objects. This allows arguments to accept arbitrarily
/// complex structs.
/// </para>
/// <para>
/// https://graphql.github.io/graphql-spec/June2018/#sec-Input-Objects
/// </para>
/// </summary>
public sealed class InputValueDefinitionNode : NamedSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="InputValueDefinitionNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the input field.
    /// </param>
    /// <param name="description">
    /// The description of the input field.
    /// </param>
    /// <param name="type">
    /// The type of this input field.
    /// </param>
    /// <param name="defaultValue">
    /// The default value of this input field.
    /// </param>
    /// <param name="directives">
    /// The directives of this input object.
    /// </param>
    public InputValueDefinitionNode(
        Location? location,
        NameNode name,
        StringValueNode? description,
        ITypeNode type,
        IValueNode? defaultValue,
        IReadOnlyList<DirectiveNode> directives)
        : base(location, name, directives)
    {
        Description = description;
        Type = type ?? throw new ArgumentNullException(nameof(type));
        DefaultValue = defaultValue;
    }

    /// <inheritdoc />
    public override SyntaxKind Kind => SyntaxKind.InputValueDefinition;

    /// <summary>
    /// Gets the description of this input field.
    /// </summary>
    public StringValueNode? Description { get; }

    /// <summary>
    /// Gets the type of this input field.
    /// </summary>
    public ITypeNode Type { get; }

    /// <summary>
    /// Gets the default value of this input field.
    /// </summary>
    public IValueNode? DefaultValue { get; }

    /// <inheritdoc />
    public override IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Description is not null)
        {
            yield return Description;
        }

        yield return Name;
        yield return Type;

        if (DefaultValue is not null)
        {
            yield return DefaultValue;
        }

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
    public InputValueDefinitionNode WithLocation(Location? location)
        => new(location, Name, Description, Type, DefaultValue, Directives);

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
    public InputValueDefinitionNode WithName(NameNode name)
        => new(Location, name, Description, Type, DefaultValue, Directives);

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
    public InputValueDefinitionNode WithDescription(StringValueNode? description)
        => new(Location, Name, description, Type, DefaultValue, Directives);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Type" /> with <paramref name="type" />.
    /// </summary>
    /// <param name="type">
    /// The type that shall be used to replace the current type.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="type" />.
    /// </returns>
    public InputValueDefinitionNode WithType(ITypeNode type)
        => new(Location, Name, Description, type, DefaultValue, Directives);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="DefaultValue" /> with <paramref name="defaultValue" />.
    /// </summary>
    /// <param name="defaultValue">
    /// The default value that shall be used to replace the current <see cref="DefaultValue" />.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="defaultValue" />.
    /// </returns>
    public InputValueDefinitionNode WithDefaultValue(IValueNode defaultValue)
        => new(Location, Name, Description, Type, defaultValue, Directives);

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
    public InputValueDefinitionNode WithDirectives(IReadOnlyList<DirectiveNode> directives)
        => new(Location, Name, Description, Type, DefaultValue, directives);
}
