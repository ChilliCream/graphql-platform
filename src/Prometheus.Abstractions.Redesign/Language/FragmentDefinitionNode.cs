using System.Collections.Generic;

namespace Prometheus.Language
{
    public class FragmentDefinitionNode
        : IExecutableDefinitionNode
    {
        public NodeKind Kind { get; } = NodeKind.FragmentDefinition;
        public Location Location { get; }
        public NameNode Name { get; }
        public NamedTypeNode TypeCondition { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public SelectionSetNode SelectionSet { get; }
    }
}