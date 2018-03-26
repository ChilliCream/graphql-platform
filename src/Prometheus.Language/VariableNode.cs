namespace Prometheus.Language
{
    public class VariableNode
        : IValueNode
    {
        public NodeKind Kind { get; } = NodeKind.Variable;
        public Location Location { get; }
        public NameNode Name { get; }
    }
}