using System.Collections.Generic;

namespace Prometheus.Language
{
    public class EnumTypeDefinitionNode
        : ITypeDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.EnumTypeDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<EnumValueDefinitionNode> Values { get; }
    }
}