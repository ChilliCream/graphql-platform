using System;

namespace Prometheus.Language
{
    public class NamedTypeNode
        : INullableType
    {
        public NamedTypeNode(Location location, NameNode name)
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
    }
}