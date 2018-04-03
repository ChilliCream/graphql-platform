using System;

namespace Prometheus.Language
{
    public class EnumValueNode
        : IValueNode
    {
        public EnumValueNode(Location location, string value)
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