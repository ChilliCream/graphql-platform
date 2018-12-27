using HotChocolate.Language;

namespace HotChocolate.Types.Factories
{
    internal interface ITypeFactory<in TNode, out TType>
        where TNode : ISyntaxNode
        where TType : IType
    {
        TType Create(TNode node);
    }
}
