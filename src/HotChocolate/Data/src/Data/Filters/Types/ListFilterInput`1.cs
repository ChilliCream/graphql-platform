using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class ListFilterInput<T>
        : FilterInputType
        , IListFilterInput
        where T : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultOperations.All).Type(typeof(T));
            descriptor.Operation(DefaultOperations.None).Type(typeof(T));
            descriptor.Operation(DefaultOperations.Some).Type(typeof(T));
            descriptor.Operation(DefaultOperations.Any).Type<BooleanType>();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }
}
