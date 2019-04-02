using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public class DefinitionBase<T>
        : DefinitionBase
        , IHasSyntaxNode
        where T : class, ISyntaxNode
    {
        protected DefinitionBase() { }

        /// <summary>
        /// The associated syntax node from the GraphQL schema SDL.
        /// </summary>
        public T SyntaxNode { get; set; }

        /// <summary>
        /// The associated syntax node from the GraphQL schema SDL.
        /// </summary>
        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
    }
}
