using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public class ComparableOperationInput<T> : FilterInputType, IComparableOperationInput
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultOperations.Equals)
                .Type(typeof(T))
                .IsNullable();
            descriptor.Operation(DefaultOperations.NotEquals)
                .Type(typeof(T))
                .IsNullable();
            descriptor.Operation(DefaultOperations.In)
                .Type(typeof(IEnumerable<T>))
                .IsNullable();
            descriptor.Operation(DefaultOperations.NotIn)
                .Type(typeof(IEnumerable<T>))
                .IsNullable();
            descriptor.Operation(DefaultOperations.GreaterThan)
                .Type(typeof(T))
                .IsNullable();
            descriptor.Operation(DefaultOperations.NotGreaterThan)
                .Type(typeof(T))
                .IsNullable();
            descriptor.Operation(DefaultOperations.GreaterThanOrEquals)
                .Type(typeof(T))
                .IsNullable();
            descriptor.Operation(DefaultOperations.NotGreaterThanOrEquals)
                .Type(typeof(T))
                .IsNullable();
            descriptor.Operation(DefaultOperations.LowerThan)
                .Type(typeof(T))
                .IsNullable();
            descriptor.Operation(DefaultOperations.NotLowerThan)
                .Type(typeof(T))
                .IsNullable();
            descriptor.Operation(DefaultOperations.LowerThanOrEquals)
                .Type(typeof(T))
                .IsNullable();
            descriptor.Operation(DefaultOperations.NotLowerThanOrEquals)
                .Type(typeof(T))
                .IsNullable();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }
}