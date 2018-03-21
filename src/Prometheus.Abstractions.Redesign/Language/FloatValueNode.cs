namespace Prometheus.Language
{
    public class FloatValueNode
      : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.IntValue;
        public Location Location { get; }
        public string Value { get; }
    }
}