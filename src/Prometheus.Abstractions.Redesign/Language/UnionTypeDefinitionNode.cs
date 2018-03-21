using System.Collections.Generic;

namespace Prometheus.Language
{
    public class UnionTypeDefinitionNode
      : ITypeDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.UnionTypeDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<NamedTypeNode> Types { get; }
    }
}