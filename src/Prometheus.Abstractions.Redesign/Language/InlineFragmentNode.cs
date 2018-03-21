using System.Collections.Generic;

namespace Prometheus.Language
{
    public class InlineFragmentNode
        : ISelectionNode
    {
        public NodeKind Kind { get; } = NodeKind.InlineFragment;
        public Location Location { get; }
        public NamedTypeNode TypeCondition { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public SelectionSetNode SelectionSet { get; }
    }
}