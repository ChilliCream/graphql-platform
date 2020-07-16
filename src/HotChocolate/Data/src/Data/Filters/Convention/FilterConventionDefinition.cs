using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters
{
    public class FilterConventionDefinition
    {
        public IEnumerable<FilterOperationConventionDefinition> Operations { get; set; } =
            Enumerable.Empty<FilterOperationConventionDefinition>();

        public Dictionary<Type, Type> Bindings { get; set; } = new Dictionary<Type, Type>();

        public Dictionary<ITypeReference, List<Action<IFilterInputTypeDescriptor>>> Extensions
        { get; private set; } = null!;

        public string? Scope { get; set; }
    }
}
