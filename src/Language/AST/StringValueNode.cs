using System;

namespace HotChocolate.Language
{
    public sealed class StringValueNode
        : IValueNode
    {
        public StringValueNode(string value)
            : this(null, value, false)
        {
        }

        public StringValueNode(
            Location location,
            string value,
            bool block)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Location = location;
            Value = value;
            Block = block;
        }

        public NodeKind Kind { get; } = NodeKind.StringValue;
        public Location Location { get; }
        public string Value { get; }
        public bool Block { get; }
    }
}
