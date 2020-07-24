namespace HotChocolate.Data.Filters
{
    public class EnumOperationInput<T> : FilterInputType
        where T : struct
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Operations.Equals).Type(typeof(T));
            descriptor.Operation(Operations.NotEquals).Type(typeof(T));
            descriptor.Operation(Operations.In).Type(typeof(T));
            descriptor.Operation(Operations.NotIn).Type(typeof(T));
            descriptor.UseAnd(false).UseOr(false);
        }
    }
}