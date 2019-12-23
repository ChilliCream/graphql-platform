using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace HotChocolate.Types.Filters.Configuration
{
    public delegate bool TryCreateImplicitFilter(
            PropertyInfo property,
            out FilterFieldDefintion definition);

    public delegate void ConfigureFilterOperation(
          IDictionary<int, TryCreateImplicitFilter> infereFilters);

    public class FilterOptionsModifiers
    {
        public IList<ConfigureFilterOperation> InfereFilters { get; } =
            new List<ConfigureFilterOperation>();

    }
}
