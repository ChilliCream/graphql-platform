using System.Collections.Generic;

namespace Prometheus.Language
{
    public class UnionTypeExtensionNode
        : ITypeExtensionNode
    {
        public NodeKind Kind { get; } = NodeKind.UnionTypeExtension;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<NamedTypeNode> Types { get; }
    }
}