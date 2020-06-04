using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionDefinition
    {
        public IReadOnlyList<TryCreateImplicitFilter> ImplicitFilters { get; set; } =
            new List<TryCreateImplicitFilter>();

        public IReadOnlyDictionary<int, IReadOnlyCollection<int>> AllowedOperations
        { get; set; } = ImmutableDictionary<int, IReadOnlyCollection<int>>.Empty;

        public IReadOnlyDictionary<int, FilterConventionTypeDefinition> TypeDefinitions
        { get; set; } = ImmutableDictionary<int, FilterConventionTypeDefinition>.Empty;

        public IReadOnlyDictionary<int, CreateFieldName> DefaultOperationNames
        { get; set; } = ImmutableDictionary<int, CreateFieldName>.Empty;

        public IReadOnlyDictionary<int, string> DefaultOperationDescriptions
        { get; set; } = ImmutableDictionary<int, string>.Empty;

        public NameString ArgumentName { get; set; }

        public NameString ElementName { get; set; }

        public FilterVisitorDefinitionBase? VisitorDefinition { get; set; }

        public GetFilterTypeName FilterTypeNameFactory { get; set; } =
            FilterConventionExtensions.FilterTypeName;

        public GetFilterTypeDescription FilterTypeDescriptionFactory { get; set; } =
            FilterConventionExtensions.FilterTypeDescription;
    }
}
