using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class RequiredModifierNode
    : INullabilityModifierNode
    , IEquatable<RequiredModifierNode>
{
    public RequiredModifierNode(Location? location, ListNullabilityNode? element)
    {
        Location = location;
        Element = element;
    }

    /// <inheritdoc />
    public SyntaxKind Kind => SyntaxKind.RequiredModifier;

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

    public bool Equals(RequiredModifierNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Equals(Element, other.Element);
    }

    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) ||
            (obj is RequiredModifierNode other && Equals(other));

    public override int GetHashCode()
        => HashCode.Combine(Kind, Element);

    public static bool operator ==(RequiredModifierNode? left, RequiredModifierNode? right)
        => Equals(left, right);

    public static bool operator !=(RequiredModifierNode? left, RequiredModifierNode? right)
        => !Equals(left, right);
}
