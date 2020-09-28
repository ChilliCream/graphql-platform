using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class ListTypeNode
        : INullableTypeNode
        , IEquatable<ListTypeNode>
    {
        public ListTypeNode(ITypeNode type)
            : this(null, type)
        {
        }

        public ListTypeNode(Location? location, ITypeNode type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Location = location;
            Type = type;
        }

        public SyntaxKind Kind { get; } = SyntaxKind.ListType;

        public Location? Location { get; }

        public ITypeNode Type { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Type;
        }

        public ListTypeNode WithLocation(Location? location)
        {
            return new ListTypeNode(location, Type);
        }

        public ListTypeNode WithType(ITypeNode type)
        {
            return new ListTypeNode(Location, type);
        }

        public bool Equals(ListTypeNode? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Type.Equals(other.Type);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as ListTypeNode);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Type.GetHashCode() * 397;
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
    }
}
