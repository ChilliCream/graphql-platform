using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class NonNullTypeNode
        : ITypeNode
        , IEquatable<NonNullTypeNode>
    {
        public NonNullTypeNode(INullableTypeNode type)
            : this(null, type)
        {
        }

        public NonNullTypeNode(Location? location, INullableTypeNode type)
        {
            Location = location;
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public SyntaxKind Kind { get; } = SyntaxKind.NonNullType;

        public Location? Location { get; }

        public INullableTypeNode Type { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Type;
        }

        public bool Equals(NonNullTypeNode? other)
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

            return Equals(obj as NonNullTypeNode);
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

        public NonNullTypeNode WithLocation(Location? location)
        {
            return new NonNullTypeNode(location, Type);
        }

        public NonNullTypeNode WithType(INullableTypeNode type)
        {
            return new NonNullTypeNode(Location, type);
        }
    }
}
