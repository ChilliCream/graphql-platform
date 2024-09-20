using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types;

public interface IInputObjectTypeDescriptor
    : IDescriptor<InputObjectTypeDefinition>
    , IFluent
{
    IInputObjectTypeDescriptor Name(string value);

    IInputObjectTypeDescriptor Description(string value);

    IInputFieldDescriptor Field(string name);

    IInputObjectTypeDescriptor Directive<T>(T directiveInstance)
        where T : class;

    IInputObjectTypeDescriptor Directive<T>()
        where T : class, new();

    IInputObjectTypeDescriptor Directive(
        string name,
        params ArgumentNode[] arguments);
}
