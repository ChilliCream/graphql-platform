using System;

namespace HotChocolate.Language
{
    public sealed class FloatValueNode
        : IValueNode
    {
        public FloatValueNode(
            Location location,
            string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(
                    "The value of a float value node cannot be null or empty.",
                    nameof(value));
            }

            Location = location;
            Value = value;
        }

        public NodeKind Kind { get; } = NodeKind.FloatValue;
        public Location Location { get; }
        public string Value { get; }
    }
}