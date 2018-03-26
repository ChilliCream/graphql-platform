using System.Collections.Generic;

namespace Prometheus.Language
{
    public class DirectiveDefinitionNode
        : ITypeSystemDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.DirectiveDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<InputValueDefinitionNode> Arguments { get; }
        public IReadOnlyCollection<NameNode> Locations { get; }
    }
}