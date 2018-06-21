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
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Location = location;
            Type = type;
        }

        public NodeKind Kind { get; } = NodeKind.NonNullType;
        public Location Location { get; }
        public INullableType Type { get; }

        public override string ToString()
        {
            return $"{Type.ToString()}!";
        }
    }
}
