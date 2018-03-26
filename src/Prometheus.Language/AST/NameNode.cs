namespace Prometheus.Language
{
    public class NameNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.Name;
        public Location Location { get; }
        public string Value { get; }
    }
}