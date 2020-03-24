﻿using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class NameNode
        : ISyntaxNode
        , IEquatable<NameNode>
    {
        public NameNode(string value)
            : this(null, value)
        {
        }

        public NameNode(Location? location, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "The value of a name node cannot be null or empty.",
                    nameof(value));
            }

            Location = location;
            Value = value;
        }

        public NodeKind Kind { get; } = NodeKind.Name;

        public Location? Location { get; }

        public string Value { get; }

        public IEnumerable<ISyntaxNode> GetNodes() => Enumerable.Empty<ISyntaxNode>();

        /// <summary>
        /// Determines whether the specified <see cref="NameNode"/>
        /// is equal to the current <see cref="NameNode"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="NameNode"/> to compare with the current
        /// <see cref="NameNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="NameNode"/> is equal
        /// to the current <see cref="NameNode"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(NameNode? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.Value.Equals(Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to
        /// the current <see cref="NameNode"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="NameNode"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal to the
        /// current <see cref="NameNode"/>; otherwise, <c>false</c>.
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

            return Equals(obj as NameNode);
        }

        /// <summary>
        /// Serves as a hash function for a <see cref="NameNode"/>
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

        public NameNode WithLocation(Location? location)
        {
            return new NameNode(location, Value);
        }

        public NameNode WithValue(string value)
        {
            return new NameNode(Location, value);
        }
    }
}
