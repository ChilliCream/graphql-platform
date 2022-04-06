using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language;

public sealed class ObjectValueNode
    : IValueNode<IReadOnlyList<ObjectFieldNode>>
        , IEquatable<ObjectValueNode>
{
    public ObjectValueNode(
        params ObjectFieldNode[] fields)
        : this(null, fields)
    {
    }

    public ObjectValueNode(
        IReadOnlyList<ObjectFieldNode> fields)
        : this(null, fields)
    {
    }

    public ObjectValueNode(
        Location? location,
        IReadOnlyList<ObjectFieldNode> fields)
    {
        Location = location;
        Fields = fields ?? throw new ArgumentNullException(nameof(fields));
                 throw new ArgumentNullException(nameof(fields));
    }

    public SyntaxKind Kind => SyntaxKind.ObjectValue;

    public Location? Location { get; }

    public IReadOnlyList<ObjectFieldNode> Fields { get; }

    IReadOnlyList<ObjectFieldNode> IValueNode<IReadOnlyList<ObjectFieldNode>>.Value => Fields;

    object IValueNode.Value => Fields;

    public IEnumerable<ISyntaxNode> GetNodes() => Fields;

    /// <summary>
    /// Determines whether the specified <see cref="ObjectValueNode"/>
    /// is equal to the current <see cref="ObjectValueNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="ObjectValueNode"/> to compare with the current
    /// <see cref="ObjectValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="ObjectValueNode"/> is equal
    /// to the current <see cref="ObjectValueNode"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(ObjectValueNode? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(other, this))
        {
            return true;
        }

        return EqualityHelper.Equals(other.Fields, Fields);
    }

    /// <summary>
    /// Determines whether the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="ObjectValueNode"/>.
    /// </summary>
    /// <param name="other">
    /// The <see cref="IValueNode"/> to compare with the current
    /// <see cref="ObjectValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
    /// to the current <see cref="ObjectValueNode"/>;
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

        if (other is ObjectValueNode o)
        {
            return Equals(o);
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to
    /// the current <see cref="ObjectValueNode"/>.
    /// </summary>
    /// <param name="obj">
    /// The <see cref="object"/> to compare with the current
    /// <see cref="ObjectValueNode"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="object"/> is equal to the
    /// current <see cref="ObjectValueNode"/>; otherwise, <c>false</c>.
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

        return Equals(obj as ObjectValueNode);
    }

    /// <summary>
    /// Serves as a hash function for a <see cref="ObjectValueNode"/>
    /// object.
    /// </summary>
    /// <returns>
    /// A hash code for this instance that is suitable for use in
    /// hashing algorithms and data structures such as a hash table.
    /// </returns>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Kind);
        hashCode.AddNodes(Fields);
        return hashCode.ToHashCode();
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

    public ObjectValueNode WithLocation(Location? location) => new(location, Fields);

    public ObjectValueNode WithFields(IReadOnlyList<ObjectFieldNode> fields) =>
        new(Location, fields);
}
