using System.Collections.Generic;

namespace Prometheus.Language
{
    public class FieldDefinitionNode
      : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.FieldDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<InputValueDefinitionNode> Arguments { get; }
        public ITypeNode Type { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}