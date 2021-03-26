using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions
{
    /// <summary>
    /// A type system definition is used in the type initialization to store properties
    /// of a type system object.
    /// </summary>
    public class DefinitionBase<T>
        : DefinitionBase
        , IHasSyntaxNode
        where T : class, ISyntaxNode
    {
        protected DefinitionBase() { }

        /// <summary>
        /// The associated syntax node from the GraphQL schema SDL.
        /// </summary>
        public T? SyntaxNode { get; set; }

        /// <summary>
        /// The associated syntax node from the GraphQL schema SDL.
        /// </summary>
        ISyntaxNode? IHasSyntaxNode.SyntaxNode => SyntaxNode;

        protected void CopyTo(DefinitionBase<T> target)
        {
            base.CopyTo(target);

            target.SyntaxNode = SyntaxNode;
        }

        protected void MergeInto(DefinitionBase<T> target)
        {
            base.MergeInto(target);

            // Note: we will not change SyntaxNode on merge.
        }
    }
}
