using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate
{
    internal interface ITypeFactory<in TNode, out TType>
        where TNode : ISyntaxNode
        where TType : IType
    {
        TType Create(SchemaContext context, TNode node);
    }
}
