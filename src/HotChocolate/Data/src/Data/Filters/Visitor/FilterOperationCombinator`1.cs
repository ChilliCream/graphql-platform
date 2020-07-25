using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterOperationCombinator<T>
        : FilterOperationCombinator
    {
        public abstract bool TryCombineOperations(
            IEnumerable<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined);

        public override bool TryCombineOperations<TOperation>(
            IEnumerable<TOperation> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out TOperation combined)
        {
            if (operations is IEnumerable<T> operationsOfT &&
                TryCombineOperations(operationsOfT, combinator, out T combinedOfT) &&
                combinedOfT is TOperation combinedOperation)
            {
                combined = combinedOperation;
                return false;
            }
            combined = default;
            return false;
        }
    }
}