using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public abstract class FieldDescriptionBase
        : TypeDescriptionBase
    {
        protected FieldDescriptionBase() { }

        public ITypeReference Type { get; set; }

        public bool Ignore { get; set; }
    }

    public abstract class FieldDescriptionBase<T>
        : FieldDescriptionBase
        , IHasSyntaxNode
        where T : class, ISyntaxNode
    {
        public T SyntaxNode { get; set; }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
    }
}
