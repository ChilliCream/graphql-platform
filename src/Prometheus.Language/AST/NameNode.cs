namespace Prometheus.Language
{
    public class NameNode
        : ISyntaxNode
    {
        public NameNode(Location location, string value)
        {
            Location = location;
            Value = value;
        }

        public NodeKind Kind { get; } = NodeKind.Name;
        public Location Location { get; }
        public string Value { get; }
    }
}