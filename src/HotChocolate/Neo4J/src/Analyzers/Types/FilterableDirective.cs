using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class FilterableDirective
    {
        public FilterableDirective(IReadOnlyList<FilterOperation> operations)
        {
            Operations = operations;
        }

        public IReadOnlyList<FilterOperation> Operations { get; }
    }
}
