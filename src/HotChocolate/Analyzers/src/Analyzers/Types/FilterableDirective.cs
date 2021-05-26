using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public class FilterableDirective
    {
        public IReadOnlyList<FilterOperation> Operations { get; set; } = default!;
    }
}
