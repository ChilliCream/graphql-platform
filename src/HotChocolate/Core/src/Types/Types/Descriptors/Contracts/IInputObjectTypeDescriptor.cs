using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IInputObjectTypeDescriptor
        : IDescriptor<InputObjectTypeDefinition>
        , IFluent
    {
        IInputObjectTypeDescriptor SyntaxNode(
            InputObjectTypeDefinitionNode inputObjectTypeDefinitionNode);

        IInputObjectTypeDescriptor Name(NameString value);

        IInputObjectTypeDescriptor Description(string value);

        IInputFieldDescriptor Field(NameString name);

        IInputObjectTypeDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IInputObjectTypeDescriptor Directive<T>()
            where T : class, new();

        IInputObjectTypeDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
