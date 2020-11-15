using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public class ComparableOperationFilterInput<T>
        : FilterInputType
        , IComparableOperationFilterInput
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultOperations.Equals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultOperations.NotEquals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultOperations.In)
                .Type(typeof(IEnumerable<T>))
                .MakeNullable();
            descriptor.Operation(DefaultOperations.NotIn)
                .Type(typeof(IEnumerable<T>))
                .MakeNullable();
            descriptor.Operation(DefaultOperations.GreaterThan)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultOperations.NotGreaterThan)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultOperations.GreaterThanOrEquals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultOperations.NotGreaterThanOrEquals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultOperations.LowerThan)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultOperations.NotLowerThan)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultOperations.LowerThanOrEquals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.Operation(DefaultOperations.NotLowerThanOrEquals)
                .Type(typeof(T))
                .MakeNullable();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }
}
