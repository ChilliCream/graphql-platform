using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IInputUnionTypeDescriptor
        : IDescriptor<InputUnionTypeDefinition>
        , IFluent
    {
        IInputUnionTypeDescriptor SyntaxNode(
            InputUnionTypeDefinitionNode inputUnionTypeDefinitionNode);

        IInputUnionTypeDescriptor Name(NameString value);

        IInputUnionTypeDescriptor Description(string value);

        IInputUnionTypeDescriptor Type<TInputObjectType>()
            where TInputObjectType : InputObjectType;

        IInputUnionTypeDescriptor Type<TInputObjectType>(TInputObjectType objectType)
            where TInputObjectType : InputObjectType;

        IInputUnionTypeDescriptor Type(NamedTypeNode objectType);

        IInputUnionTypeDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IInputUnionTypeDescriptor Directive<T>()
            where T : class, new();

        IInputUnionTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
