using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterOperationCombinator
    {
        public abstract bool TryCombineOperations<T>(
            IEnumerable<T> operations,
            FilterCombinator combinator,
            [NotNullWhen(true)] out T combined);
    }
}
