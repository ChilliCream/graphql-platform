using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IComparableFilterFieldDescriptor
        : IFluent
    {
        IComparableFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        IComparableFilterFieldDescriptor BindExplicitly();

        IComparableFilterFieldDescriptor BindImplicitly();


        IComparableFilterOperationDescriptor AllowEquals();
        IComparableFilterOperationDescriptor AllowNotEquals();


        IComparableFilterOperationDescriptor AllowIn();

        IComparableFilterOperationDescriptor AllowNotIn();


        IComparableFilterOperationDescriptor AllowGreaterThan();
        IComparableFilterOperationDescriptor AllowNotGreaterThan();

        IComparableFilterOperationDescriptor AllowGreaterThanOrEquals();
        IComparableFilterOperationDescriptor AllowNotGreaterThanOrEquals();


        IComparableFilterOperationDescriptor AllowLowerThan();
        IComparableFilterOperationDescriptor AllowNotLowerThan();


        IComparableFilterOperationDescriptor AllowLowerThanOrEquals();
        IComparableFilterOperationDescriptor AllowNotLowerThanOrEquals();
    }
}
