using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class BooleanOperationFilterInputType
        : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<BooleanType>();
            descriptor.Operation(DefaultFilterOperations.NotEquals).Type<BooleanType>();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }
}
