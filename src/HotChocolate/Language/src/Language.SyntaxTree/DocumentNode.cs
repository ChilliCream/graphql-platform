using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

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
            Location? location,
            IReadOnlyList<IDefinitionNode> definitions)
        {
            Location = location;
            Definitions = definitions
                ?? throw new ArgumentNullException(nameof(definitions));
        }

        public SyntaxKind Kind { get; } = SyntaxKind.Document;

        public Location? Location { get; }

        public IReadOnlyList<IDefinitionNode> Definitions { get; }

        public IEnumerable<ISyntaxNode> GetNodes() => Definitions;

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public override string ToString() => SyntaxPrinter.Print(this, true);

        /// <summary>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </summary>
        /// <param name="indented">
        /// A value that indicates whether the GraphQL output should be formatted,
        /// which includes indenting nested GraphQL tokens, adding
        /// new lines, and adding white space between property names and values.
        /// </param>
        /// <returns>
        /// Returns the GraphQL syntax representation of this <see cref="ISyntaxNode"/>.
        /// </returns>
        public string ToString(bool indented) => SyntaxPrinter.Print(this, indented);

        public DocumentNode WithLocation(Location? location)
        {
            return new DocumentNode(location, Definitions);
        }

        public DocumentNode WithDefinitions(
            IReadOnlyList<IDefinitionNode> definitions)
        {
            return new DocumentNode(Location, definitions);
        }

        public static DocumentNode Empty { get; } =
            new DocumentNode(null, Array.Empty<IDefinitionNode>());
    }
}
