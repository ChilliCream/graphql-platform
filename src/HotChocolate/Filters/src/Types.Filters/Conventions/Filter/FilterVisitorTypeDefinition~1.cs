using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterVisitorTypeDefinition<T>
    {
        public FilterKind FilterKind { get; set; }

        public FilterFieldEnter<T>? Enter { get; set; }

        public FilterFieldLeave<T>? Leave { get; set; }

        public IReadOnlyDictionary<FilterOperationKind, FilterOperationHandler<T>> OperationHandlers
        { get; set; } = ImmutableDictionary<FilterOperationKind, FilterOperationHandler<T>>.Empty;
    }
}
