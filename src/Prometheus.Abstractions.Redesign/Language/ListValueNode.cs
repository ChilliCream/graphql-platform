using System.Collections.Generic;

namespace Prometheus.Language
{
    public class ListValueNode
      : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.ListValue;
        public Location Location { get; }
        public IReadOnlyCollection<IValueNode> Value { get; }
    }
}