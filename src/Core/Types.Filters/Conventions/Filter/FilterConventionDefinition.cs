using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionDefinition
    {
        public IList<TryCreateImplicitFilter> ImplicitFilters { get; set; }

        public IDictionary<AllowedFilterType, FilterOperationKind> AllowedOperations { get; set; }

        public IDictionary<FilterOperationKind, CreateFieldName> Names { get; set; }

        public IDictionary<FilterOperationKind, NameString> Descriptions { get; set; }

        public NameString ArgumentName { get; set; }

        public NameString ArrayFilterPropertyName { get; set; }

        public GetFilterTypeName GetFilterTypeName { get; set; }

    }
}
