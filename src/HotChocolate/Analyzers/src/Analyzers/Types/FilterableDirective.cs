using System.Collections.Generic;

namespace HotChocolate.Analyzers.Types
{
    public class FilterableDirective
    {
        public IReadOnlyList<FilterOperation> Operations { get; set; } = default!;
    }
}
