using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public class FilterProviderDefinition : IHasScope
    {
        public string? Scope { get; set; }
    }
}
