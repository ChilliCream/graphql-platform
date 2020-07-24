namespace HotChocolate.Data.Filters
{
    public class ListFilterInput<T> : FilterInputType
        where T : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Operations.All).Type(typeof(T));
            descriptor.Operation(Operations.None).Type(typeof(T));
            descriptor.Operation(Operations.Some).Type(typeof(T));
            descriptor.Operation(Operations.Any).Type<BooleanOperationInput>();
            descriptor.UseAnd(false).UseOr(false);
        }
    }
}