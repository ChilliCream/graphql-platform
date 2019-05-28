using System;
using System.Collections.Generic;

namespace HotChocolate.Language
{
    public sealed class DocumentNode
        : ISyntaxNode
    {
        public DocumentNode(IReadOnlyList<IDefinitionNode> definitions)
            : this(null, definitions)
        {
        }

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

        public DocumentNode WithLocation(Location location)
        {
            return new DocumentNode(location, Definitions);
        }

        public DocumentNode WithDefinitions(
            IReadOnlyList<IDefinitionNode> definitions)
        {
            return new DocumentNode(Location, definitions);
        }

#if NETSTANDARD1_2
        public static DocumentNode Empty { get; } =
            new DocumentNode(null, new IDefinitionNode[0]);
#else
        public static DocumentNode Empty { get; } =
            new DocumentNode(null, Array.Empty<IDefinitionNode>());
#endif
    }
}
