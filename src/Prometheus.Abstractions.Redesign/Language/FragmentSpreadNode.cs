using System.Collections.Generic;

namespace Prometheus.Language
{
    public class FragmentSpreadNode
      : ISelectionNode
    {
        public NodeKind Kind { get; } = NodeKind.FragmentSpread;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
    }
}