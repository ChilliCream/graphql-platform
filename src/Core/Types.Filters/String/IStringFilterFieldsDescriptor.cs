using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{

    public interface IStringFilterFieldDescriptor
    {
        IStringFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        IStringFilterDescriptor AllowContains();

        IStringFilterDescriptor AllowEquals();

        IStringFilterDescriptor AllowIn();
        IStringFilterDescriptor AllowStartsWith();

        IStringFilterDescriptor AllowEndsWith();
    }

    public interface IStringFilterDescriptor
        : IDescriptor<InputFieldDefinition>
        , IFluent
    {
        IStringFilterFieldDescriptor And();

        IStringFilterDescriptor Name(NameString value);

        IStringFilterDescriptor Description(string value);

        IStringFilterDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IStringFilterDescriptor Directive<T>()
            where T : class, new();

        IStringFilterDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
