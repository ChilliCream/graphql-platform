using System.Collections.Generic;

namespace Prometheus.Language
{
    public class DocumentNode
        : ISyntaxNode
    {
        public DocumentNode(Location location, IReadOnlyCollection<IDefinitionNode> definitions)
        {
            if (definitions == null)
            {
                throw new System.ArgumentNullException(nameof(definitions));
            }

            Location = location;
            Definitions = definitions;
        }

        public NodeKind Kind { get; } = NodeKind.Document;
        public Location Location { get; }
        public IReadOnlyCollection<IDefinitionNode> Definitions { get; }
    }
}