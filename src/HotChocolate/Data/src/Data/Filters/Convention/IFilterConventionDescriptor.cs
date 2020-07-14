namespace HotChocolate.Data.Filters
{
    public interface IFilterConventionDescriptor
    {
        IFilterOperationConventionDescriptor Operation(int operation);

        IFilterConventionDescriptor Binding<TRuntime, TInput>();
    }
}
