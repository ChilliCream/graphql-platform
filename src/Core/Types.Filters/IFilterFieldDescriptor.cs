using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IFilterFieldDescriptor
        : IDescriptor<InputFieldDefinition>
        , IFluent
    {
        IInputFieldDescriptor Name(NameString value);

        IInputFieldDescriptor Description(string value);

        IInputFieldDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IInputFieldDescriptor Directive<T>()
            where T : class, new();

        IInputFieldDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
