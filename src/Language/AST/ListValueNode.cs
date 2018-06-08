using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ListValueNode
        : IValueNode
    {
        public ListValueNode(
            IReadOnlyList<IValueNode> items)
            : this(null, items)
        {
        }

        public ListValueNode(
            Location location,
            IReadOnlyList<IValueNode> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            Location = location;
            Items = items;
        }

        public NodeKind Kind { get; } = NodeKind.ListValue;
        public Location Location { get; }
        public IReadOnlyList<IValueNode> Items { get; }
    }
}
