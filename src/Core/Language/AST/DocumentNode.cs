using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class DocumentNode
        : ISyntaxNode
    {
        public DocumentNode(
            Location location,
            IReadOnlyCollection<IDefinitionNode> definitions)
        {
            Location = location;
            Definitions = definitions 
                ?? throw new ArgumentNullException(nameof(definitions));
        }

        public NodeKind Kind { get; } = NodeKind.Document;

        public Location Location { get; }

        public IReadOnlyCollection<IDefinitionNode> Definitions { get; }

        public DocumentNode WithLocation(Location location)
        {
            return new DocumentNode(location, Definitions);
        }

        public DocumentNode WithDefinitions(
            IReadOnlyCollection<IDefinitionNode> definitions)
        {
            return new DocumentNode(Location, definitions);
        }

        public static DocumentNode Empty { get; } =
            new DocumentNode(null, Array.Empty<IDefinitionNode>());
    }
}
