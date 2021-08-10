using System.Collections.Generic;

namespace HotChocolate.CodeGeneration.Types
{
    public class FilterableDirective
    {
        public IReadOnlyList<FilterOperation> Operations { get; set; } = default!;
    }
}
