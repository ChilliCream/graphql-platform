using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public class FilterProviderDefinition
    {
        public List<(Type Handler, IFilterFieldHandler? HandlerInstance)> Handlers { get; } =
            new List<(Type Handler, IFilterFieldHandler? HandlerInstance)>();
    }
}
