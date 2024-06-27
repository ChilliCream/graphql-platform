using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters;

public class StringOperationFilterInputType : FilterInputType
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Operation(DefaultFilterOperations.Equals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.NotEquals).Type<StringType>();
        descriptor.Operation(DefaultFilterOperations.Contains).Type<StringType>().Expensive();
        descriptor.Operation(DefaultFilterOperations.NotContains).Type<StringType>().Expensive();
        descriptor.Operation(DefaultFilterOperations.In).Type<ListType<StringType>>();
        descriptor.Operation(DefaultFilterOperations.NotIn).Type<ListType<StringType>>();
        descriptor.Operation(DefaultFilterOperations.StartsWith).Type<StringType>().Expensive();
        descriptor.Operation(DefaultFilterOperations.NotStartsWith).Type<StringType>().Expensive();
        descriptor.Operation(DefaultFilterOperations.EndsWith).Type<StringType>().Expensive();
        descriptor.Operation(DefaultFilterOperations.NotEndsWith).Type<StringType>().Expensive();
    }
}

file static class Extensions
{
    public static IFilterOperationFieldDescriptor Expensive(this IFilterOperationFieldDescriptor descriptor)
    {
        var definition = descriptor.Extend().Definition;

        if ((definition.Flags & FieldFlags.FilterOperationField) == FieldFlags.FilterOperationField)
        {
            definition.Flags &= ~FieldFlags.FilterOperationField;
        }

        definition.Flags |= FieldFlags.FilterExpensiveOperationField;

        return descriptor;
    }
}
