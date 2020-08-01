using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public class EnumOperationInput<T> : FilterInputType, IEnumOperationInput
        where T : struct
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Operations.Equals).Type(typeof(T));
            descriptor.Operation(Operations.NotEquals).Type(typeof(T));
            descriptor.Operation(Operations.In).Type(typeof(IEnumerable<T>));
            descriptor.Operation(Operations.NotIn).Type(typeof(IEnumerable<T>));
            descriptor.UseAnd(false).UseOr(false);
        }
    }
}