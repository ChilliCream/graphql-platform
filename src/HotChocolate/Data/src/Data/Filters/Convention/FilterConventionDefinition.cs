using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Filters
{
    public class FilterConventionDefinition
    {
        public IEnumerable<FilterOperationConventionDefinition> Operations { get; set; } =
            Enumerable.Empty<FilterOperationConventionDefinition>();

        public string? Scope { get; set; }
    }
}
