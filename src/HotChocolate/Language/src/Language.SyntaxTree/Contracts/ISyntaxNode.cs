using System.Collections.Generic;

namespace HotChocolate.Language
{
    /// <summary>
    /// Represents a non-terminal node in the GraphQL syntax tree.
    /// </summary>
    public interface ISyntaxNode
    {
        /// <summary>
        /// Returns the <see cref="SyntaxKind"/> of the node.
        /// </summary>
        SyntaxKind Kind { get; }

        /// <summary>
        /// Gets a <see cref="Location"/> for this node if available.
        /// </summary>
        Location? Location { get; }

        /// <summary>
        /// Gets the children of this node.
        /// </summary>
        /// <returns>
        /// Returns the children of this node..
        /// </returns>
        IEnumerable<ISyntaxNode> GetNodes();

        /// <summary>
        /// Generates a string representation of the current GraphQL syntax node.
        /// </summary>
        /// <returns>
        /// Returns a string representation of the current GraphQL syntax node.
        /// </returns>
        string ToString();

        /// <summary>
        /// Generates a string representation of the current GraphQL syntax node.
        /// </summary>
        /// <param name="indented">
        /// Specifies if the GraphQL string representation shall contain indentations.
        /// </param>
        /// <returns>
        /// Returns a string representation of the current GraphQL syntax node.
        /// </returns>
        string ToString(bool indented);
    }
}
