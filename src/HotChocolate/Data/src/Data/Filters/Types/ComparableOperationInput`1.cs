using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public class ComparableOperationInput<T> : FilterInputType, IComparableOperationInput
        where T : IComparable

    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultOperations.Equals).Type(typeof(T));
            descriptor.Operation(DefaultOperations.NotEquals).Type(typeof(T));
            descriptor.Operation(DefaultOperations.In).Type(typeof(IEnumerable<T>));
            descriptor.Operation(DefaultOperations.NotIn).Type(typeof(IEnumerable<T>));
            descriptor.Operation(DefaultOperations.GreaterThan).Type(typeof(T));
            descriptor.Operation(DefaultOperations.NotGreaterThan).Type(typeof(T));
            descriptor.Operation(DefaultOperations.GreaterThanOrEquals).Type(typeof(T));
            descriptor.Operation(DefaultOperations.NotGreaterThanOrEquals).Type(typeof(T));
            descriptor.Operation(DefaultOperations.LowerThan).Type(typeof(T));
            descriptor.Operation(DefaultOperations.NotLowerThan).Type(typeof(T));
            descriptor.Operation(DefaultOperations.LowerThanOrEquals).Type(typeof(T));
            descriptor.Operation(DefaultOperations.NotLowerThanOrEquals).Type(typeof(T));
            descriptor.UseAnd(false).UseOr(false);
        }
    }
}