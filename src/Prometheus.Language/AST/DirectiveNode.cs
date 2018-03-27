using System.Collections.Generic;

namespace Prometheus.Language
{
    public class DirectiveNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.Directive;
        public Location Location { get; }
        public NameNode Name { get; }
        public IReadOnlyCollection<ArgumentNode> Arguments { get; }
    }
}