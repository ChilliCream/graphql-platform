using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

/// <summary>
/// Represents a boolean value literal.
/// The two keywords true and false represent the two boolean values.
/// https://spec.graphql.org/October2021/#sec-Boolean-Value
/// </summary>
public sealed class BooleanValueNode
    : IValueNode<bool>
    , IEquatable<BooleanValueNode?>
{
    /// <summary>
    /// Initializes a new instance of <see cref="BooleanValueNode"/>
    /// </summary>
    /// <param name="value">
    /// The boolean value.
    /// </param>
    public BooleanValueNode(bool value)
        : this(null, value)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="BooleanValueNode"/>
    /// </summary>
    /// <param name="location">
    /// The location of the named syntax node within the original source text.
    /// </param>
    /// <param name="value">
    /// The boolean value.
    /// </param>
    public BooleanValueNode(
        Location? location,
        bool value)
    {
        Location = location;
        Value = value;
    }

    /// <inheritdoc cref="ISyntaxNode" />
    public SyntaxKind Kind => SyntaxKind.BooleanValue;

    /// <inheritdoc cref="ISyntaxNode" />
    public Location? Location { get; }

    /// <summary>
    /// The runtime value of this value literal.
    /// </summary>
    public bool Value { get; }

    /// <summary>
    /// The runtime value of this value literal.
    /// </summary>
    object IValueNode.Value => Value;

    /// <inheritdoc cref="ISyntaxNode" />
    public IEnumerable<ISyntaxNode> GetNodes() => Enumerable.Empty<ISyntaxNode>();

    /// <summary>
    /// Determines whether the specified <see cref="BooleanValueNode"/>
    /// is equal to the current <see cref="BooleanValueNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="BooleanValueNode"/> to compare with the current
    /// <see cref="BooleanValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="BooleanValueNode"/> is equal
    /// to the current <see cref="BooleanValueNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(BooleanValueNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(other, this))
        {
            return true;
        }

        return other.Value.Equals(Value);
    }

    /// <summary>
    /// Determines whether the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="BooleanValueNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="IValueNode"/> to compare with the current
    /// <see cref="BooleanValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="BooleanValueNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(IValueNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(other, this))
        {
            return true;
        }

        if (other is BooleanValueNode b)
        {
            return Equals(b);
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="BooleanValueNode"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="BooleanValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="BooleanValueNode"/>; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        return Equals(obj as BooleanValueNode);
    }

    /// <summary>
    /// Serves as a hash function for a <see cref="BooleanValueNode"/>
    /// object.
    /// </summary>
    /// <returns>
    /// A hash code for this instance that is suitable for use in
    /// hashing algorithms and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return (Kind.GetHashCode() * 397)
             ^ (Value.GetHashCode() * 97);
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
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Location" /> with <paramref name="location" />.
    /// </summary>
    /// <param name="location">
    /// The location that shall be used to replace the current location.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="location" />.
    /// </returns>
    public BooleanValueNode WithLocation(Location? location)
        => new(location, Value);

    /// <summary>
    /// Creates a new node from the current instance and replaces the
    /// <see cref="Value" /> with <paramref name="value" />.
    /// </summary>
    /// <param name="value">
    /// The value that shall be used to replace the current value.
    /// </param>
    /// <returns>
    /// Returns the new node with the new <paramref name="value" />.
    /// </returns>
    public BooleanValueNode WithValue(bool value)
        => new(Location, value);

    /// <summary>
    /// Represents the true value for the boolean literal.
    /// </summary>
    public static BooleanValueNode True { get; } = new(true);

    /// <summary>
    /// Represents the false value for the boolean literal.
    /// </summary>
    public static BooleanValueNode False { get; } = new(false);
}
