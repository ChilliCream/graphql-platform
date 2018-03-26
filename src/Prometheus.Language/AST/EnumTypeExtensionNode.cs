using System.Collections.Generic;

namespace Prometheus.Language
{
    public class EnumTypeExtensionNode
        : ITypeDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.EnumTypeExtension;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<EnumValueDefinitionNode> Values { get; }
    }
}