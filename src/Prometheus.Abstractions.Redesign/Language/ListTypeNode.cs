namespace Prometheus.Language
{
    public class ListTypeNode
      : INullableType
    {
        public NodeKind Kind { get; } = NodeKind.ListType;
        public Location Location { get; }
        public ITypeNode Type { get; }
    }
}