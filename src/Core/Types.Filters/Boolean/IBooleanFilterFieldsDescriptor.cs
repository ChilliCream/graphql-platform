using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IBooleanFilterFieldDescriptor
        : IFluent
    {
        IBooleanFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        IBooleanFilterFieldDescriptor BindExplicitly();

        IBooleanFilterFieldDescriptor BindImplicitly(); 
        IBooleanFilterOperationDescriptor AllowEquals();
        IBooleanFilterOperationDescriptor AllowNotEquals();
    }
}
