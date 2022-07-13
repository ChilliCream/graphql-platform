using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public interface IUnionTypeDescriptor
    : IDescriptor<UnionTypeDefinition>
    , IFluent
{
    IUnionTypeDescriptor SyntaxNode(UnionTypeDefinitionNode unionTypeDefinition);

    IUnionTypeDescriptor Name(string value);

    IUnionTypeDescriptor Description(string value);

    IUnionTypeDescriptor Type<TObjectType>() where TObjectType : ObjectType;

    IUnionTypeDescriptor Type<TObjectType>(TObjectType objectType) where TObjectType : ObjectType;

    IUnionTypeDescriptor Type(NamedTypeNode objectType);

    IUnionTypeDescriptor ResolveAbstractType(ResolveAbstractType resolveAbstractType);

    IUnionTypeDescriptor Directive<T>(T directiveInstance) where T : class;

    IUnionTypeDescriptor Directive<T>() where T : class, new();

    IUnionTypeDescriptor Directive(string name, params ArgumentNode[] arguments);
}
