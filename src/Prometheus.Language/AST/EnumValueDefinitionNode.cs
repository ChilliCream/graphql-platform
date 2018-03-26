using System.Collections.Generic;

namespace Prometheus.Language
{
    public class EnumValueDefinitionNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.EnumValueDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public StringValueNode Description { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}