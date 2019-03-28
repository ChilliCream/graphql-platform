using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IHasSyntaxNode
    {
        /// <summary>
        /// The associated syntax node from the GraphQL schema SDL.
        /// </summary>
        ISyntaxNode SyntaxNode { get; }
    }
}
