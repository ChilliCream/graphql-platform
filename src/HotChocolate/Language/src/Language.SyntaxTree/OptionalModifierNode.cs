using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class OptionalModifierNode
    : INullabilityModifierNode
    , IEquatable<OptionalModifierNode>
{
    public OptionalModifierNode(ListNullabilityNode element) : this(null, element) { }

    public OptionalModifierNode(Location location) : this(location, null) { }

    public OptionalModifierNode(Location? location, ListNullabilityNode? element)
    {
        Location = location;
        Element = element;
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.OptionalModifier;

    /// <inheritdoc />
    public Location? Location { get; }

    /// <inheritdoc />
    public ListNullabilityNode? Element { get; }

    INullabilityNode? INullabilityNode.Element => Element;

    /// <inheritdoc />
    public IEnumerable<ISyntaxNode> GetNodes()
    {
        if (Element is not null)
        {
            yield return Element;
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
    public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

    /// <summary>
    /// Determines whether the specified <see cref="OptionalModifierNode"/>
    /// is equal to the current <see cref="OptionalModifierNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="OptionalModifierNode"/> to compare with the current
    /// <see cref="OptionalModifierNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="OptionalModifierNode"/> is equal
    /// to the current <see cref="OptionalModifierNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(OptionalModifierNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Element.IsEqualTo(other.Element);
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="OptionalModifierNode"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="OptionalModifierNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="OptionalModifierNode"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => Equals(obj as OptionalModifierNode);

    /// <summary>
    /// Serves as a hash function for a <see cref="OptionalModifierNode"/>
    /// object.
    /// </summary>
    /// <returns>
    /// A hash code for this instance that is suitable for use in
    /// hashing algorithms and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode()
       => HashCode.Combine(Kind, Element);

    public static bool operator ==(
        OptionalModifierNode? left,
        OptionalModifierNode? right)
        => Equals(left, right);

    public static bool operator !=(
        OptionalModifierNode? left,
        OptionalModifierNode? right)
        => !Equals(left, right);
}
