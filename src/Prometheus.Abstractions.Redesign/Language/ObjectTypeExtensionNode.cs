using System.Collections.Generic;

namespace Prometheus.Language
{
    public class ObjectTypeExtensionNode
    : ITypeExtensionNode
    {
        public NodeKind Kind { get; } = NodeKind.ObjectTypeExtension;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<NamedTypeNode> Interfaces { get; }
        public IReadOnlyCollection<FieldDefinitionNode> Fields { get; }
    }
}