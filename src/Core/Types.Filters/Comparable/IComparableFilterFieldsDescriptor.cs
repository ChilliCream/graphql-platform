using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{

    public interface IComparableFilterFieldDescriptor
    {
        IComparableFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);


        IComparableFilterFieldDetailsDescriptor AllowEquals();

        IComparableFilterFieldDetailsDescriptor AllowIn();
        IComparableFilterFieldDetailsDescriptor AllowGreaterThan();
        IComparableFilterFieldDetailsDescriptor AllowGreaterThanOrEquals();
        IComparableFilterFieldDetailsDescriptor AllowLowerThan();
        IComparableFilterFieldDetailsDescriptor AllowLowerThanOrEquals();
         
    }

    public interface IComparableFilterFieldDetailsDescriptor
        : IDescriptor<InputFieldDefinition>
        , IFluent
    {
        IComparableFilterFieldDescriptor And();

        IComparableFilterFieldDetailsDescriptor Name(NameString value);

        IComparableFilterFieldDetailsDescriptor Description(string value);

        IComparableFilterFieldDetailsDescriptor Directive<T>(T directiveInstance)
            where T : class;

        IComparableFilterFieldDetailsDescriptor Directive<T>()
            where T : class, new();

        IComparableFilterFieldDetailsDescriptor Directive(
            NameString name,
            params ArgumentNode[] arguments);
    }
}
