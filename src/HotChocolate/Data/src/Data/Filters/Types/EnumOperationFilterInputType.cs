using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class EnumOperationFilterInputType<T> : FilterInputType, IEnumOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotEquals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.In)
                .Type(typeof(IEnumerable<T>))
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotIn)
                .Type(typeof(IEnumerable<T>))
                .MakeNullable();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }
}
