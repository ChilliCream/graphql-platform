using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class ListFilterInput<T> : FilterInputType, IListFilterInputType
        where T : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Operations.All).Type(typeof(T));
            descriptor.Operation(Operations.None).Type(typeof(T));
            descriptor.Operation(Operations.Some).Type(typeof(T));
            descriptor.Operation(Operations.Any).Type<BooleanType>();
            descriptor.UseAnd(false).UseOr(false);
        }
    }
}