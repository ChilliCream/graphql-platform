using System;

namespace Prometheus.Language
{
    public class StringValueNode
        : IValueNode
    {
        public StringValueNode(Location location, string value, bool block)
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