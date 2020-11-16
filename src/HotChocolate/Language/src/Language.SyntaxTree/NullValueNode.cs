using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class NullValueNode
        : IValueNode<object?>
        , IEquatable<NullValueNode>
    {
        private const string _null = "null";

        private NullValueNode()
        {
        }

        public NullValueNode(Location? location)
        {
            Location = location;
        }

        public SyntaxKind Kind { get; } = SyntaxKind.NullValue;

        public Location? Location { get; }

        public object? Value { get; }

        public IEnumerable<ISyntaxNode> GetNodes() => Enumerable.Empty<ISyntaxNode>();

        /// <summary>
        /// Determines whether the specified <see cref="NullValueNode"/>
        /// is equal to the current <see cref="NullValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="NullValueNode"/> to compare with the current
        /// <see cref="NullValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="NullValueNode"/> is equal
        /// to the current <see cref="NullValueNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(NullValueNode? other)
        {
            if (other is null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="NullValueNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="IValueNode"/> to compare with the current
        /// <see cref="NullValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="IValueNode"/> is equal
        /// to the current <see cref="NullValueNode"/>;
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

            return other is NullValueNode;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="NullValueNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="NullValueNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="NullValueNode"/>; otherwise, <c>false</c>.
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

            return obj is NullValueNode;
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="NullValueNode"/>
        /// object.
        /// </summary>
        /// <returns>
        /// A hash code for this instance that is suitable for use in
        /// hashing algorithms and data structures such as a hash table.
        /// </returns>
        public override int GetHashCode() => 104729;

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

        public static NullValueNode Default { get; } = new NullValueNode();

        public NullValueNode WithLocation(Location? location)
        {
            return new NullValueNode(location);
        }
    }
}
