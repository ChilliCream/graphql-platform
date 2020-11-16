using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterOperationCombinator
    {
        public abstract bool TryCombineOperations<TContext, T>(
            TContext context,
            Queue<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined)
            where TContext : FilterVisitorContext<T>;
    }
}
