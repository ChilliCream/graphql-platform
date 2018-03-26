namespace Prometheus.Language
{
    public class VariableDefinitionNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.VariableDefinition;
        public Location Location { get; }
        public VariableNode Variable { get; }
        public ITypeNode Type { get; }
        public IValueNode DefaultValue { get; }
    }
}