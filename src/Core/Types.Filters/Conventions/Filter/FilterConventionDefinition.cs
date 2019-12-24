using System.Collections.Generic;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionDefinition
    {
        public IList<TryCreateImplicitFilter> ImplicitFilters { get; set; }

        public IDictionary<FilterKind, ISet<FilterOperationKind>> AllowedOperations { get; set; }

        public IDictionary<FilterKind, FilterConventionTypeDefinition> TypeDefinitions
        { get; set; }

        public IDictionary<FilterOperationKind, CreateFieldName> DefaultOperationNames
        { get; set; }

        public IDictionary<FilterOperationKind, NameString> DefaultOperationDescriptions
        { get; set; }

        public NameString ArgumentName { get; set; }

        public NameString ArrayFilterPropertyName { get; set; }

        public GetFilterTypeName GetFilterTypeName { get; set; }

    }
}
