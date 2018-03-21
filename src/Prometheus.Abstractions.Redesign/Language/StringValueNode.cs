namespace Prometheus.Language
{
    public class StringValueNode
        : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.StringValue;
        public Location Location { get; }
        public string Value { get; }
        public bool? Block { get; }
    }
}