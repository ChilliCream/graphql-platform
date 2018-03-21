using System.Collections.Generic;

namespace Prometheus.Language
{
    public class ScalarTypeExtensionNode
    : ITypeExtensionNode
    {
        public NodeKind Kind { get; } = NodeKind.ScalarTypeExtension;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}