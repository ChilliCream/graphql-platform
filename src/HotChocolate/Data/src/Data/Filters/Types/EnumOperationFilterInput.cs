using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public class EnumOperationFilterInput<T> : FilterInputType, IEnumOperationFilterInput
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultOperations.Equals).Type(typeof(T)).MakeNullable();
            descriptor.Operation(DefaultOperations.NotEquals).Type(typeof(T)).MakeNullable();
            descriptor.Operation(DefaultOperations.In).Type(typeof(IEnumerable<T>)).MakeNullable();
            descriptor.Operation(DefaultOperations.NotIn)
                .Type(typeof(IEnumerable<T>))
                .MakeNullable();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }
}
