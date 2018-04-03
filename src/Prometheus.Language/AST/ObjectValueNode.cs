using System;
using System.Collections.Generic;

namespace Prometheus.Language
{
    public class ObjectValueNode
        : IValueNode
    {
        public ObjectValueNode(Location location, 
            IReadOnlyCollection<ObjectFieldNode> fields)
        {
            if (fields == null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            Location = location;
            Fields = fields;
        }

        public NodeKind Kind { get; } = NodeKind.ObjectValue;
        public Location Location { get; }
        public IReadOnlyCollection<ObjectFieldNode> Fields { get; }
    }
}