using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public abstract class FieldDescriptionBase<T>
        : FieldDescriptionBase
        , IHasSyntaxNode
        where T : class, ISyntaxNode
    {
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
