using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors
{
    public abstract class TypeDescriptionBase<T>
        : TypeDescriptionBase
        , IHasSyntaxNode
        where T : class, ISyntaxNode
    {
        public T SyntaxNode { get; set; }

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
    }
}
