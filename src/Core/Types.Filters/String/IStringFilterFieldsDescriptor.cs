using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IStringFilterFieldDescriptor
        : IFluent
    {
        IStringFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        IStringFilterFieldDescriptor BindExplicitly();

        IStringFilterFieldDescriptor BindImplicitly();

        IStringFilterOperationDescriptor AllowContains();
        IStringFilterOperationDescriptor AllowNotContains();

        IStringFilterOperationDescriptor AllowEquals();
        IStringFilterOperationDescriptor AllowNotEquals();

        IStringFilterOperationDescriptor AllowIn();
        IStringFilterOperationDescriptor AllowNotIn();

        IStringFilterOperationDescriptor AllowStartsWith();
        IStringFilterOperationDescriptor AllowNotStartsWith();

        IStringFilterOperationDescriptor AllowEndsWith();
        IStringFilterOperationDescriptor AllowNotEndsWith();
    }
}
