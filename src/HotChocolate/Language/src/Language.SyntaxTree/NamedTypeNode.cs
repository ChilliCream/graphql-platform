using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    public sealed class NamedTypeNode
        : INullableTypeNode
        , IEquatable<NamedTypeNode>
    {
        public NamedTypeNode(string name)
            : this(null, new NameNode(name))
        {
        }

        public NamedTypeNode(NameNode name)
            : this(null, name)
        {
        }

        public NamedTypeNode(Location? location, NameNode name)
        {
            Location = location;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public SyntaxKind Kind { get; } = SyntaxKind.NamedType;

        public Location? Location { get; }

        public NameNode Name { get; }

        public IEnumerable<ISyntaxNode> GetNodes()
        {
            yield return Name;
        }

        public NamedTypeNode WithLocation(Location? location)
        {
            return new NamedTypeNode(location, Name);
        }

        public NamedTypeNode WithName(NameNode name)
        {
            return new NamedTypeNode(Location, name);
        }

        public bool Equals(NamedTypeNode? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Name.Value.Equals(
                other.Name.Value,
                StringComparison.Ordinal);
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

            return Equals(obj as NamedTypeNode);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Name.GetHashCode() * 397;
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
