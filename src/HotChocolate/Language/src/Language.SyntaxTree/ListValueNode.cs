using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// <para>
/// Represents list value syntax.
/// </para>
/// <para>
/// Lists are ordered sequences of values wrapped in square-brackets [ ].
/// The values of a List literal may be any value literal or variable (ex. [1, 2, 3]).
/// </para>
/// <para>
/// Commas are optional throughout GraphQL so trailing commas are allowed and
/// repeated commas do not represent missing values.
/// </para>
/// </summary>
public sealed class ListValueNode : IValueNode<IReadOnlyList<IValueNode>>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ListValueNode"/>.
    /// </summary>
    /// <param name="item">
    /// The item that shall be the only item of this list.
    /// </param>
    public ListValueNode(IValueNode item)
        : this(default(Location?), item)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ListValueNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="item">
    /// The item that shall be the only item of this list.
    /// </param>
    public ListValueNode(Location? location, IValueNode item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        Location = location;
        Items = new IValueNode[] { item, };
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ListValueNode"/>.
    /// </summary>
    /// <param name="items">
    /// The items of this list.
    /// </param>
    public ListValueNode(
        IReadOnlyList<IValueNode> items)
        : this(null, items)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ListValueNode"/>.
    /// </summary>
    /// <param name="items">
    /// The items of this list.
    /// </param>
    public ListValueNode(params IValueNode[] items)
        : this(null, items)
    {
    }
    /// <summary>
    /// Initializes a new instance of <see cref="ListValueNode"/>.
    /// </summary>
    /// <param name="location">
    /// The location of the syntax node within the original source text.
    /// </param>
    /// <param name="items">
    /// The items of this list.
    /// </param>
    public ListValueNode(
        Location? location,
        IReadOnlyList<IValueNode> items)
    {
        Location = location;
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.ListValue;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <summary>
    /// The items of this list.
    /// </summary>
    public IReadOnlyList<IValueNode> Items { get; }

    IReadOnlyList<IValueNode> IValueNode<IReadOnlyList<IValueNode>>.Value => Items;

    object IValueNode.Value => Items;

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes() => Items;

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
    public ListValueNode WithLocation(Location? location) => new(location, Items);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Items" /> with <paramref name="items" />.
    /// </summary>
    /// <param name="items">
    /// The <paramref name="items" /> that shall be used to replace the current <see cref="Items"/>.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="items" />.
    /// </returns>
    public ListValueNode WithItems(IReadOnlyList<IValueNode> items) => new(Location, items);
}
