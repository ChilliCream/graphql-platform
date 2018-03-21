using System.Collections.Generic;

namespace Prometheus.Language
{
    public class ObjectValueNode
      : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.ObjectValue;
        public Location Location { get; }
        public IReadOnlyCollection<ObjectFieldNode> Fields { get; }
    }
}