using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors.Definitions
{
    public abstract class FieldDefinitionBase<T>
        : FieldDefinitionBase
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

        protected void CopyTo(FieldDefinitionBase<T> target)
        {
            base.CopyTo(target);

            target.SyntaxNode = SyntaxNode;
        }

        protected void MergeInto(FieldDefinitionBase<T> target)
        {
            base.MergeInto(target);

            // Note: we are not copying the SyntaxNode on merge.
        }
    }
}
