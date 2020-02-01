using System;

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

        public NodeKind Kind { get; } = NodeKind.NonNullType;

        public Location? Location { get; }

        public INullableTypeNode Type { get; }

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

        public override string? ToString()
        {
            return $"{Type.ToString()}!";
        }

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
