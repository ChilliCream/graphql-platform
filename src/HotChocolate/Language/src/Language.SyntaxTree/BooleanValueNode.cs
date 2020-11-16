using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class BooleanValueNode
        : IValueNode<bool>
        , IEquatable<BooleanValueNode?>
    {
        public BooleanValueNode(bool value)
            : this(null, value)
        {
        }

        public BooleanValueNode(
            Location? location,
            bool value)
        {
            Location = location;
            Value = value;
        }

        public SyntaxKind Kind { get; } = SyntaxKind.BooleanValue;

        public Location? Location { get; }

        public bool Value { get; }

        object IValueNode.Value => Value;

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

        public BooleanValueNode WithLocation(Location? location) =>
            new BooleanValueNode(location, Value);

        public BooleanValueNode WithValue(bool value) =>
            new BooleanValueNode(Location, value);

        public static BooleanValueNode True { get; } = new BooleanValueNode(true);

        public static BooleanValueNode False { get; } = new BooleanValueNode(false);
    }
}
