namespace Prometheus.Language
{
    public class BooleanValueNode
        : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.BooleanValue;
        public Location Location { get; }
        public bool Value { get; }
    }
}