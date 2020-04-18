using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionDefinition
    {
        public IReadOnlyList<TryCreateImplicitFilter> ImplicitFilters { get; set; }
            = new List<TryCreateImplicitFilter>();

        public IReadOnlyDictionary<FilterKind, IReadOnlyCollection<FilterOperationKind>>
            AllowedOperations
        {
            get; set;
        }
            = ImmutableDictionary<FilterKind, IReadOnlyCollection<FilterOperationKind>>.Empty;

        public IReadOnlyDictionary<FilterKind, FilterConventionTypeDefinition> TypeDefinitions
        {
            get; set;
        } = ImmutableDictionary<FilterKind, FilterConventionTypeDefinition>.Empty;

        public IReadOnlyDictionary<FilterOperationKind, CreateFieldName> DefaultOperationNames
        {
            get; set;
        } = ImmutableDictionary<FilterOperationKind, CreateFieldName>.Empty;

        public IReadOnlyDictionary<FilterOperationKind, string> DefaultOperationDescriptions
        {
            get; set;
        } = ImmutableDictionary<FilterOperationKind, string>.Empty;

        public NameString ArgumentName { get; set; }

        public NameString ElementName { get; set; }

        public FilterVisitorDefinitionBase? VisitorDefinition { get; set; }

        public GetFilterTypeName FilterTypeNameFactory { get; set; }
            = FilterConventionExtensions.FilterTypeName;

        public GetFilterTypeDescription FilterTypeDescriptionFactory { get; set; }
            = FilterConventionExtensions.FilterTypeDescription;
    }
}
