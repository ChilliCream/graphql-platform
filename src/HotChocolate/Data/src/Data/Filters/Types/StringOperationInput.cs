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
            descriptor.Operation(Operations.In).Type<ListType<StringType>>();
            descriptor.Operation(Operations.NotIn).Type<ListType<StringType>>();
            descriptor.Operation(Operations.StartsWith).Type<StringType>();
            descriptor.Operation(Operations.NotStartsWith).Type<StringType>();
            descriptor.Operation(Operations.EndsWith).Type<StringType>();
            descriptor.Operation(Operations.NotEndsWith).Type<StringType>();
        }
    }
}