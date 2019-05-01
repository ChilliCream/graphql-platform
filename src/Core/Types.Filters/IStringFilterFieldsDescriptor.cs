namespace HotChocolate.Types.Filters
{

    public interface IStringFilterFieldsDescriptor
    {
        IStringFilterFieldsDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        IStringFilterFieldsDescriptor AllowContains();

        IStringFilterFieldsDescriptor AllowEquals();

        IStringFilterFieldsDescriptor AllowIn();
    }
}
