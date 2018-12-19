using System;

namespace HotChocolate.Language
{
    public sealed class NonNullTypeNode
        : ITypeNode
    {
        public NonNullTypeNode(
            Location location,
            INullableType type)
        {
            Location = location;
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public NodeKind Kind { get; } = NodeKind.NonNullType;

        public Location Location { get; }

        public INullableType Type { get; }

        public override string ToString()
        {
            return $"{Type.ToString()}!";
        }

        public NonNullTypeNode WithLocation(Location location)
        {
            return new NonNullTypeNode(location, Type);
        }

        public NonNullTypeNode WithType(INullableType type)
        {
            return new NonNullTypeNode(Location, Type);
        }
    }
}
