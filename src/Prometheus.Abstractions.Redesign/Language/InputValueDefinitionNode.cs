using System.Collections.Generic;

namespace Prometheus.Language
{
    public class InputValueDefinitionNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.InputValueDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public ITypeNode Type { get; }
        public IValueNode Value { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}