using System;

namespace HotChocolate.Language
{
    public sealed class ObjectFieldNode
        : ISyntaxNode
    {
        public ObjectFieldNode(
            Location location,
            NameNode name,
            IValueNode value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Location = location;
            Name = name;
            Value = value;
        }

        public NodeKind Kind { get; } = NodeKind.ObjectField;
        public Location Location { get; }
        public NameNode Name { get; }
        public IValueNode Value { get; }
    }
}