using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class BooleanOperationInput : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Operations.Equals).Type<BooleanType>();
            descriptor.Operation(Operations.NotEquals).Type<BooleanType>();
        }
    }
}