using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterOperationCombinator<TContext, T>
        : FilterOperationCombinator
        where TContext : FilterVisitorContext<T>
    {
        public abstract bool TryCombineOperations(
            TContext context,
            Queue<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined);

        public override bool TryCombineOperations<TVisitorContext, TOperation>(
            TVisitorContext context,
            Queue<TOperation> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out TOperation combined)
        {
            if (operations is Queue<T> operationsOfT &&
                context is TContext contextOfT &&
                TryCombineOperations(
                    contextOfT,
                    operationsOfT,
                    combinator,
                    out T combinedOfT) &&
                combinedOfT is TOperation combinedOperation)
            {
                combined = combinedOperation;
                return true;
            }

            combined = default!;
            return false;
        }
    }
}
