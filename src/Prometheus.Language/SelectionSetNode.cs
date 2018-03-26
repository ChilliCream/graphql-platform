using System.Collections.Generic;

namespace Prometheus.Language
{
    public class SelectionSetNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.SelectionSet;
        public Location Location { get; }
        public IReadOnlyCollection<ISelectionNode> Selections { get; }
    }
}