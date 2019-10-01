using System;

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

        public NodeKind Kind { get; } = NodeKind.NamedType;

        public Location? Location { get; }

        public NameNode Name { get; }

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

        public override string? ToString()
        {
            return Name.Value;
        }
    }
}
