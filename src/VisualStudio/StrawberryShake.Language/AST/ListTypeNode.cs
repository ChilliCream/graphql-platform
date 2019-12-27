using System;

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

        public NodeKind Kind { get; } = NodeKind.ListType;

        public Location? Location { get; }

        public ITypeNode Type { get; }

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

        public override string? ToString()
        {
            return $"[{Type.ToString()}]";
        }
    }
}
