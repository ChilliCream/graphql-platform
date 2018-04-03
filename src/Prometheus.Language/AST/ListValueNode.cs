using System.Collections.Generic;

namespace Prometheus.Language
{
    public class ListValueNode
        : IValueNode
    {
        public ListValueNode(Location location, IReadOnlyCollection<IValueNode> items)
        {
            if (items == null)
            {
                throw new System.ArgumentNullException(nameof(items));
            }

            Location = location;
            Items = items;
        }

        public NodeKind Kind { get; } = NodeKind.ListValue;
        public Location Location { get; }
        public IReadOnlyCollection<IValueNode> Items { get; }
    }
}