using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class ComparableOperationFilterInputType<T>
        : FilterInputType
        , IComparableOperationFilterInputType
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
            descriptor.Operation(DefaultFilterOperations.GreaterThan)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThan)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.GreaterThanOrEquals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThanOrEquals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.LowerThan)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotLowerThan)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.LowerThanOrEquals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotLowerThanOrEquals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }
}
