using System;

namespace Prometheus.Language
{
    public class VariableNode
        : IValueNode
    {
        public VariableNode(Location location, NameNode name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Location = location;
            Name = name;
        }

        public NodeKind Kind { get; } = NodeKind.Variable;
        public Location Location { get; }
        public NameNode Name { get; }
    }
}