using System;
using System.Globalization;

namespace HotChocolate.Language
{
    public sealed class IntValueNode
        : IValueNode<string>
    {
        public IntValueNode(int value)
            : this(null, value.ToString(CultureInfo.InvariantCulture))
        {
        }

        public IntValueNode(string value)
            : this(null, value)
        {
        }

        public IntValueNode(
            Location location,
            string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "The value of an int value node cannot be null or empty.",
                    nameof(value));
            }

            Location = location;
            Value = value;
        }

        public NodeKind Kind { get; } = NodeKind.IntValue;
        public Location Location { get; }
        public string Value { get; }
    }
}
