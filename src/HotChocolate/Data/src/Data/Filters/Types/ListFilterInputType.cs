using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class ListFilterInputType<T>
        : FilterInputType
        , IListFilterInputType
        where T : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.All).Type(typeof(T));
            descriptor.Operation(DefaultFilterOperations.None).Type(typeof(T));
            descriptor.Operation(DefaultFilterOperations.Some).Type(typeof(T));
            descriptor.Operation(DefaultFilterOperations.Any).Type<BooleanType>();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }
}
