using System.Collections.Generic;

namespace Prometheus.Language
{
    public class InputObjectTypeDefinitionNode
  : ITypeDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.InputObjectTypeDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<InputValueDefinitionNode> Fields { get; }

    }
}