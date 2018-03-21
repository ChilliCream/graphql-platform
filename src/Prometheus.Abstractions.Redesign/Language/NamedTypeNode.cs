namespace Prometheus.Language
{
    public class NamedTypeNode
        : INullableType
    {
        public NodeKind Kind { get; } = NodeKind.NamedType;
        public Location Location { get; }
        public NameNode Name { get; }
    }
}