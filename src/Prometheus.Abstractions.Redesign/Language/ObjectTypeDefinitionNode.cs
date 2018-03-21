using System.Collections.Generic;

namespace Prometheus.Language
{
    public class ObjectTypeDefinitionNode
        : ITypeDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.ObjectTypeDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<NamedTypeNode> Interfaces { get; }
        public IReadOnlyCollection<FieldDefinitionNode> Fields { get; }

    }
}