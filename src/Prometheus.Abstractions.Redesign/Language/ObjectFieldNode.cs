namespace Prometheus.Language
{
    public class ObjectFieldNode
      : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.ObjectField;
        public Location Location { get; }
        public NameNode Name { get; }
        public IValueNode Value { get; }
    }
}