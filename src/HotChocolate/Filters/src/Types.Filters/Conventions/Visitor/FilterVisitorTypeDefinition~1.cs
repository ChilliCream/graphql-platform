using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterVisitorTypeDefinition<T>
    {
        public object FilterKind { get; set; } = default!;

        public FilterFieldEnter<T>? Enter { get; set; }

        public FilterFieldLeave<T>? Leave { get; set; }

        public IReadOnlyDictionary<object, FilterOperationHandler<T>> OperationHandlers
        { get; set; } = ImmutableDictionary<object, FilterOperationHandler<T>>.Empty;
    }
}
