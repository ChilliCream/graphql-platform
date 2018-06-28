using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IUnionTypeDescriptor
        : IFluent
    {
        IUnionTypeDescriptor SyntaxNode(UnionTypeDefinitionNode syntaxNode);

        IUnionTypeDescriptor Name(string name);

        IUnionTypeDescriptor Description(string description);

        IUnionTypeDescriptor Type<TObjectType>()
            where TObjectType : ObjectType;

        IUnionTypeDescriptor Type(NamedTypeNode objectType);

        IUnionTypeDescriptor ResolveAbstractType(
            ResolveAbstractType resolveAbstractType);
    }
}
