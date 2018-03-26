namespace Prometheus.Language
{
    public class FloatValueNode
        : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.FloatValue;
        public Location Location { get; }
        public string Value { get; }
    }
}