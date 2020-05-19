using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterExpressionTypeDefinition
    {
        public FilterKind FilterKind { get; set; }

        public FilterFieldEnter? Enter { get; set; }

        public FilterFieldLeave? Leave { get; set; }

        public IReadOnlyDictionary<FilterOperationKind, FilterOperationHandler> OperationHandlers
        { get; set; } = ImmutableDictionary<FilterOperationKind, FilterOperationHandler>.Empty;
    }
}
