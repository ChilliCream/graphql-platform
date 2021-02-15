using System;
using System.Collections.Generic;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Language
{
    /// <summary>
    /// The <see cref="DocumentNode"/> represents a parsed GraphQL document
    /// which also is the root node of a parsed GraphQL document.
    ///
    /// The document can contain schema definition nodes or query nodes.
    /// </summary>
    public sealed class DocumentNode
        : ISyntaxNode
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DocumentNode"/>.
        /// </summary>
        /// <param name="definitions">
        /// The GraphQL definitions this document contains.
        /// </param>
        public DocumentNode(
            IReadOnlyList<IDefinitionNode> definitions)
            : this(null, definitions)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DocumentNode"/>.
        /// </summary>
        /// <param name="location">
        /// The location of the document in the parsed source text.
        /// </param>
        /// <param name="definitions">
        /// The GraphQL definitions this document contains.
        /// </param>
        public DocumentNode(
            Location? location,
            IReadOnlyList<IDefinitionNode> definitions)
        {
            Location = location;
            Definitions = definitions ??
                throw new ArgumentNullException(nameof(definitions));
        }

        /// <inheritdoc />
        public SyntaxKind Kind { get; } = SyntaxKind.Document;

        /// <inheritdoc />
        public Location? Location { get; }

        public IReadOnlyList<IDefinitionNode> Definitions { get; }

        /// <inheritdoc />
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

        /// <summary>
        /// Creates a new instance that has all the characteristics of this
        /// documents but with a different location.
        /// </summary>
        /// <param name="location">
        /// The location that shall be applied to the new document.
        /// </param>
        /// <returns>
        /// Returns a new instance that has all the characteristics of this
        /// documents but with a different location.
        /// </returns>
        public DocumentNode WithLocation(Location? location) =>
            new(location, Definitions);

        /// <summary>
        /// Creates a new instance that has all the characteristics of this
        /// documents but with different definitions.
        /// </summary>
        /// <param name="definitions">
        /// The definitions that shall be applied to the new document.
        /// </param>
        /// <returns>
        /// Returns a new instance that has all the characteristics of this
        /// documents but with a different definitions.
        /// </returns>
        public DocumentNode WithDefinitions(
            IReadOnlyList<IDefinitionNode> definitions) =>
            new(Location, definitions);

        /// <summary>
        /// Gets an empty GraphQL document.
        /// </summary>
        public static DocumentNode Empty { get; } =
            new(null, Array.Empty<IDefinitionNode>());
    }
}
