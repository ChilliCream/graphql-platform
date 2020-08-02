using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public class EnumOperationInput<T> : FilterInputType, IEnumOperationInput
        where T : struct
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultOperations.Equals).Type(typeof(T));
            descriptor.Operation(DefaultOperations.NotEquals).Type(typeof(T));
            descriptor.Operation(DefaultOperations.In).Type(typeof(IEnumerable<T>));
            descriptor.Operation(DefaultOperations.NotIn).Type(typeof(IEnumerable<T>));
            descriptor.UseAnd(false).UseOr(false);
        }
    }
}