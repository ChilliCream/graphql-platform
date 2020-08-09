using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public class EnumOperationInput<T> : FilterInputType, IEnumOperationInput
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultOperations.Equals).Type(typeof(T)).IsNullable();
            descriptor.Operation(DefaultOperations.NotEquals).Type(typeof(T)).IsNullable();
            descriptor.Operation(DefaultOperations.In).Type(typeof(IEnumerable<T>)).IsNullable();
            descriptor.Operation(DefaultOperations.NotIn).Type(typeof(IEnumerable<T>)).IsNullable();
            descriptor.UseAnd(false).UseOr(false);
        }
    }
}
