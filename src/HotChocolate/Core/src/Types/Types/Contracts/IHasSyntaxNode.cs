using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// All implementing classes might correlate to a syntax node
    /// in a parsed GraphQL SDL syntax tree.
    /// </summary>
    public interface IHasSyntaxNode
    {
        /// <summary>
        /// The associated syntax node from the GraphQL SDL.
        /// </summary>
        ISyntaxNode? SyntaxNode { get; }
    }
}
