using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// Represents a GraphQL object literal.
/// </para>
/// <para>
/// Input object literal values are unordered lists of keyed input values
/// wrapped in curly-braces { }.
/// </para>
/// <para>
/// The values of an object literal may be any input value literal or
/// variable (ex. { name: "Hello world", score: 1.0 }).
/// </para>
/// <para>We refer to literal representation of input objects as “object literals.”
/// </para>
/// </summary>
public sealed class ObjectValueNode : IValueNode<IReadOnlyList<ObjectFieldNode>>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ObjectValueNode"/>.
    /// </summary>
    /// <param name="fields">
    /// The assigned field values.
    /// </param>
    public ObjectValueNode(params ObjectFieldNode[] fields)
        : this(null, fields)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectValueNode"/>.
    /// </summary>
    /// <param name="fields">
    /// The assigned field values.
    /// </param>
    public ObjectValueNode(IReadOnlyList<ObjectFieldNode> fields)
        : this(null, fields)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectValueNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="fields">
    /// The assigned field values.
    /// </param>
    public ObjectValueNode(Location? location, IReadOnlyList<ObjectFieldNode> fields)
    {
        Location = location;
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.ObjectValue;

    /// <inheritdoc />
    public Location? Location { get; }

    public IReadOnlyList<ObjectFieldNode> Fields { get; }

    IReadOnlyList<ObjectFieldNode> IValueNode<IReadOnlyList<ObjectFieldNode>>.Value => Fields;

    object IValueNode.Value => Fields;

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes() => Fields;

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
    public ObjectValueNode WithLocation(Location? location)
        => new(location, Fields);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Fields" /> with <paramref name="fields" />.
    /// </summary>
    /// <param name="fields">
    /// The fields that shall be used to replace the current <see cref="Fields"/>.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="fields" />.
    /// </returns>
    public ObjectValueNode WithFields(IReadOnlyList<ObjectFieldNode> fields)
        => new(Location, fields);
}
