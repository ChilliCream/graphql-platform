using System.Collections.Generic;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public class FilterProviderDefiniton<T, TContext>
        where TContext : FilterVisitorContext<T>
    {
        public string Scope { get; set; } = ConventionBase.DefaultScope;

        public List<FilterFieldHandler<T, TContext>> Handlers { get; }
            = new List<FilterFieldHandler<T, TContext>>();

        public FilterVisitor<T, TContext>? Visitor { get; set; }

        public FilterOperationCombinator? Combinator { get; set; }
    }
}
