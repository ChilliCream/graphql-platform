namespace Prometheus.Language
{
    public class EnumValueNode
        : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.EnumValue;
        public Location Location { get; }
        public string Value { get; }
    }
}