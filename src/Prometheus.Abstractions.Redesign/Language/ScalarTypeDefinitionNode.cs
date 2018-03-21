using System.Collections.Generic;

namespace Prometheus.Language
{
    public class ScalarTypeDefinitionNode
      : ITypeDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.ScalarTypeDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}