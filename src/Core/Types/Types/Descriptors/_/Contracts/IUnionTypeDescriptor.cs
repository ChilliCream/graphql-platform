using HotChocolate.Language;

namespace HotChocolate.Types
{
    public interface IUnionTypeDescriptor
        : IFluent
    {
        IUnionTypeDescriptor SyntaxNode(
            UnionTypeDefinitionNode unionTypeDefinitionNode);

        IUnionTypeDescriptor Name(NameString value);

        IUnionTypeDescriptor Description(string value);

        IUnionTypeDescriptor Type<TObjectType>()
            where TObjectType : ObjectType;

        IUnionTypeDescriptor Type<TObjectType>(TObjectType objectType)
            where TObjectType : ObjectType;

        IUnionTypeDescriptor Type(NamedTypeNode objectType);

        IUnionTypeDescriptor ResolveAbstractType(
            ResolveAbstractType resolveAbstractType);

        IUnionTypeDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IUnionTypeDescriptor Directive<T>()
            where T : class, new();

        IUnionTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
