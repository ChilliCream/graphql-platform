using System;
using System.Collections.Generic;
using System.Text;

namespace HotChocolate.Types.Filters.Configuration
{
    public interface IFilterOptions
    {
        IEnumerable<TryCreateImplicitFilter> GetImplicitFilterFactories();
    }
}
