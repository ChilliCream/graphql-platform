using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IStringFilterFieldDescriptor
        : IFluent
    {
        IStringFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        IStringFilterOperationDescriptor AllowContains();

        IStringFilterOperationDescriptor AllowEquals();

        IStringFilterOperationDescriptor AllowIn();

        IStringFilterOperationDescriptor AllowStartsWith();

        IStringFilterOperationDescriptor AllowEndsWith();
    }
}
