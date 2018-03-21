namespace Prometheus.Language
{
    public class OperationTypeDefinitionNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.OperationTypeDefinition;
        public Location Location { get; }
        public OperationTypeNode Operation { get; }
        public NamedTypeNode Type { get; }
    }
}