namespace Prometheus.Language
{
    public class NonNullTypeNode
        : ITypeNode
    {
        public NodeKind Kind { get; } = NodeKind.NonNullType;
        public Location Location { get; }
        public INullableType Type { get; }
    }
}