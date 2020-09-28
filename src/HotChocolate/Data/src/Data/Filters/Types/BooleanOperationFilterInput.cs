using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class BooleanOperationFilterInput : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultOperations.Equals).Type<BooleanType>();
            descriptor.Operation(DefaultOperations.NotEquals).Type<BooleanType>();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }
}
