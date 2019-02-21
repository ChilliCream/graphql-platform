using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public abstract class FieldDescriptionBase<T>
        : FieldDescriptionBase
        , IHasSyntaxNode
        where T : class, ISyntaxNode
    {
        public T SyntaxNode { get; set; }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
    }
}
