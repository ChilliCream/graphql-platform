using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public class DescriptionBase<T>
        : DescriptionBase
        , IHasSyntaxNode
        where T : class, ISyntaxNode
    {
        protected DescriptionBase() { }

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
