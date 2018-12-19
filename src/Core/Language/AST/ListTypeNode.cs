using System;

namespace HotChocolate.Language
{
    public sealed class ListTypeNode
        : INullableType
    {
        public ListTypeNode(
            Location location,
            ITypeNode type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Location = location;
            Type = type;
        }

        public NodeKind Kind { get; } = NodeKind.ListType;

        public Location Location { get; }

        public ITypeNode Type { get; }

        public override string ToString()
        {
            return $"[{Type.ToString()}]";
        }

        public ListTypeNode WithLocation(Location location)
        {
            return new ListTypeNode(location, Type);
        }

        public ListTypeNode WithType(ITypeNode type)
        {
            return new ListTypeNode(Location, type);
        }
    }
}
