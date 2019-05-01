namespace HotChocolate.Types.Filters
{

    public interface IStringFilterFieldsDescriptor
    {
        IFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        IFilterFieldDescriptor AllowContains();

        IFilterFieldDescriptor AllowEquals();

        IFilterFieldDescriptor AllowIn();
    }
}
