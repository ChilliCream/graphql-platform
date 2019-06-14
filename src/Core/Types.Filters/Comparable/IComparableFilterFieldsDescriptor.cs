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

        IComparableFilterOperationDescriptor AllowIn();

        IComparableFilterOperationDescriptor AllowGreaterThan();
        IComparableFilterOperationDescriptor AllowGreaterThanOrEquals();

        IComparableFilterOperationDescriptor AllowLowerThan();

        IComparableFilterOperationDescriptor AllowLowerThanOrEquals();
    }
}
