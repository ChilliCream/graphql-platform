using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class ObjectValueNode
        : IValueNode
    {
        public ObjectValueNode(
            IReadOnlyCollection<ObjectFieldNode> fields)
            : this(null, fields)
        {
        }

        public ObjectValueNode(
            Location location,
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
