using System.Collections.Generic;

namespace Prometheus.Language
{
    public class InterfaceTypeExtensionNode
        : ITypeExtensionNode
    {
        public NodeKind Kind { get; } = NodeKind.InterfaceTypeExtension;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<FieldDefinitionNode> Fields { get; }

    }
}