using System.Collections.Generic;

namespace Prometheus.Language
{
    public class InterfaceTypeDefinitionNode
        : ITypeDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.InterfaceTypeDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<FieldDefinitionNode> Fields { get; }

    }
}