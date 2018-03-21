namespace Prometheus.Language
{
    public class ArgumentNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.Argument;
        public Location Location { get; }
        public NameNode Name { get; }
        public IValueNode Value { get; }
    }
}