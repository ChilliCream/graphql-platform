using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class StringOperationInput : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Operations.Equals).Type<StringType>();
            descriptor.Operation(Operations.NotEquals).Type<StringType>();
            descriptor.Operation(Operations.Contains).Type<StringType>();
            descriptor.Operation(Operations.NotContains).Type<StringType>();
            descriptor.Operation(Operations.In).Type<StringType>();
            descriptor.Operation(Operations.NotIn).Type<StringType>();
            descriptor.Operation(Operations.StartsWith).Type<StringType>();
            descriptor.Operation(Operations.NotStartsWith).Type<StringType>();
            descriptor.Operation(Operations.EndsWith).Type<StringType>();
            descriptor.Operation(Operations.NotEndsWith).Type<StringType>();
            descriptor.Operation(Operations.GreaterThan).Type<StringType>();
            descriptor.Operation(Operations.NotGreaterThan).Type<StringType>();
            descriptor.Operation(Operations.GreaterThanOrEquals).Type<StringType>();
            descriptor.Operation(Operations.NotGreaterThanOrEquals).Type<StringType>();
            descriptor.Operation(Operations.LowerThan).Type<StringType>();
            descriptor.Operation(Operations.NotLowerThan).Type<StringType>();
            descriptor.Operation(Operations.LowerThanOrEquals).Type<StringType>();
            descriptor.Operation(Operations.NotLowerThanOrEquals).Type<StringType>();
        }
    }
}