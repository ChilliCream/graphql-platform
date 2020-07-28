using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public class ComparableOperationInput<T> : FilterInputType, IComparableOperationInput
        where T : IComparable

    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(Operations.Equals).Type(typeof(T));
            descriptor.Operation(Operations.NotEquals).Type(typeof(T));
            descriptor.Operation(Operations.In).Type(typeof(IEnumerable<T>));
            descriptor.Operation(Operations.NotIn).Type(typeof(IEnumerable<T>));
            descriptor.Operation(Operations.GreaterThan).Type(typeof(T));
            descriptor.Operation(Operations.NotGreaterThan).Type(typeof(T));
            descriptor.Operation(Operations.GreaterThanOrEquals).Type(typeof(T));
            descriptor.Operation(Operations.NotGreaterThanOrEquals).Type(typeof(T));
            descriptor.Operation(Operations.LowerThan).Type(typeof(T));
            descriptor.Operation(Operations.NotLowerThan).Type(typeof(T));
            descriptor.Operation(Operations.LowerThanOrEquals).Type(typeof(T));
            descriptor.Operation(Operations.NotLowerThanOrEquals).Type(typeof(T));
            descriptor.UseAnd(false).UseOr(false);
        }
    }
}