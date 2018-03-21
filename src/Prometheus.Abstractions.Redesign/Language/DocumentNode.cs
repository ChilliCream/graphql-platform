using System.Collections.Generic;

namespace Prometheus.Language
{
    public class DocumentNode
        : ISyntaxNode
    {
        public NodeKind Kind { get; } = NodeKind.Document;
        public Location Location { get; }
        public IReadOnlyCollection<IExecutableDefinitionNode> Definitions { get; }
    }
}