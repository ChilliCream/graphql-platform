using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Configuration
{
    internal class FilterConfiguration
    {
        public FilterConfiguration()
        {
            ImplicitFilters = new Dictionary<int, TryCreateImplicitFilter>();

        }
        public IDictionary<int, TryCreateImplicitFilter> ImplicitFilters { get; set; }

    }
}
