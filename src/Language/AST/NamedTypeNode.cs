using System;

namespace HotChocolate.Language
{
    public sealed class NamedTypeNode
        : INullableType
    {
        public NamedTypeNode(
            Location location,
            NameNode name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Location = location;
            Name = name;
        }

        public NodeKind Kind { get; } = NodeKind.NamedType;
        public Location Location { get; }
        public NameNode Name { get; }

        public override string ToString()
        {
            return Name.Value;
        }
    }
}
