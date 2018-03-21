namespace Prometheus.Language
{
    public class NullValueNode
        : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.NullValue;
        public Location Location { get; }
    }
}