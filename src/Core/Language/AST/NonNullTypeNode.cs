using System;

namespace HotChocolate.Language
{
    public sealed class NonNullTypeNode
        : ITypeNode
    {
        public NonNullTypeNode(INullableTypeNode type)
            : this(null, type)
        {
        }

        public NonNullTypeNode(Location location, INullableTypeNode type)
        {
            Location = location;
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public NodeKind Kind { get; } = NodeKind.NonNullType;

        public Location Location { get; }

        public INullableTypeNode Type { get; }

        public override string ToString()
        {
            return $"{Type.ToString()}!";
        }

        public NonNullTypeNode WithLocation(Location location)
        {
            return new NonNullTypeNode(location, Type);
        }

        public NonNullTypeNode WithType(INullableTypeNode type)
        {
            return new NonNullTypeNode(Location, Type);
        }
    }
}
