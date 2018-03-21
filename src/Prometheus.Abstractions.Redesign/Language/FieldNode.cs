using System.Collections.Generic;

namespace Prometheus.Language
{
    public class FieldNode
      : ISelectionNode
    {
        public NodeKind Kind { get; } = NodeKind.Field;
        public Location Location { get; }
        public NameNode Name { get; }
        public NameNode Alias { get; }
        public IReadOnlyCollection<DirectiveNode> Directives { get; }
        public IReadOnlyCollection<ArgumentNode> Arguments { get; }
        public SelectionSetNode SelectionSet { get; }
    }
}