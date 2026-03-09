using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Factories;

internal interface ITypeFactory<in TNode, out TType>
    where TNode : ISyntaxNode
    where TType : INameProvider
{
    TType Create(IDescriptorContext context, TNode node);
}
