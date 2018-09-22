using System;

namespace HotChocolate.Language
{
    public sealed class ArgumentNode
        : ISyntaxNode
    {
        public ArgumentNode(
            string name,
            IValueNode value)
            : this(null, new NameNode(name), value)
        {
        }

        public ArgumentNode(
            NameNode name,
            IValueNode value)
            : this(null, name, value)
        {
        }

        public ArgumentNode(
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

        public NodeKind Kind { get; } = NodeKind.Argument;

        public Location Location { get; }

        public NameNode Name { get; }

        public IValueNode Value { get; }
    }
}
