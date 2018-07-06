using System;

namespace HotChocolate.Language
{
    public sealed class EnumValueNode
        : IValueNode<string>
    {
        public EnumValueNode(string value)
            : this(null, value)
        {
        }

        public EnumValueNode(
            Location location,
            string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Location = location;
            Value = value;
        }

        public NodeKind Kind { get; } = NodeKind.EnumValue;

        public Location Location { get; }

        public string Value { get; }
    }
}
