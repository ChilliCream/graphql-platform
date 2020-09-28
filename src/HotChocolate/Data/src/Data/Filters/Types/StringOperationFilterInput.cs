using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class StringOperationFilterInput : FilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultOperations.Equals).Type<StringType>();
            descriptor.Operation(DefaultOperations.NotEquals).Type<StringType>();
            descriptor.Operation(DefaultOperations.Contains).Type<StringType>();
            descriptor.Operation(DefaultOperations.NotContains).Type<StringType>();
            descriptor.Operation(DefaultOperations.In).Type<ListType<StringType>>();
            descriptor.Operation(DefaultOperations.NotIn).Type<ListType<StringType>>();
            descriptor.Operation(DefaultOperations.StartsWith).Type<StringType>();
            descriptor.Operation(DefaultOperations.NotStartsWith).Type<StringType>();
            descriptor.Operation(DefaultOperations.EndsWith).Type<StringType>();
            descriptor.Operation(DefaultOperations.NotEndsWith).Type<StringType>();
        }
    }
}
