using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represent a GraphQL variable syntax.
/// </summary>
public sealed class VariableNode : IValueNode<string>
{
    /// <summary>
    /// Initializes a new instance of <see cref="VariableNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the input object.
    /// </param>
    public VariableNode(string name)
        : this(null, new NameNode(name))
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="VariableNode"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the input object.
    /// </param>
    public VariableNode(NameNode name)
        : this(null, name)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="VariableNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="name">
    /// The name of the input object.
    /// </param>
    public VariableNode(
        Location? location,
        NameNode name)
    {
        Location = location;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.Variable;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// Gets the variable name.
    /// </summary>
    public NameNode Name { get; }

    string IValueNode<string>.Value => Name.Value;

    object IValueNode.Value => Name.Value;

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        yield return Name;
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
    public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

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
    public VariableNode WithLocation(Location? location) => new(location, Name);

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
    public VariableNode WithName(NameNode name) => new(Location, name);
}
