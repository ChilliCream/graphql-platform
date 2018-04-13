using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ListValueNode
        : IValueNode
    {
        public ListValueNode(
            Location location,
            IReadOnlyCollection<IValueNode> items)
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
        public IReadOnlyCollection<IValueNode> Items { get; }
    }
}