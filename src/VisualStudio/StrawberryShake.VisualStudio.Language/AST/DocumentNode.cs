using System;
using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public sealed class DocumentNode
        : ISyntaxNode
    {
        public DocumentNode(
            Location location,
            IReadOnlyList<IDefinitionNode> definitions)
        {
            Location = location;
            Definitions = definitions
                ?? throw new ArgumentNullException(nameof(definitions));
        }

        public NodeKind Kind { get; } = NodeKind.Document;

        public Location Location { get; }

        public IReadOnlyList<IDefinitionNode> Definitions { get; }

        public IEnumerable<ISyntaxNode> GetNodes() => Definitions;

        public DocumentNode WithLocation(Location location)
        {
            return new DocumentNode(location, Definitions);
        }

        public DocumentNode WithDefinitions(
            IReadOnlyList<IDefinitionNode> definitions)
        {
            return new DocumentNode(Location, definitions);
        }
    }
}
