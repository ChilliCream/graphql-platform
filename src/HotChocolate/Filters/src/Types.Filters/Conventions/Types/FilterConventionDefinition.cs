using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionDefinition
    {
        public IReadOnlyList<TryCreateImplicitFilter> ImplicitFilters { get; set; } =
            new List<TryCreateImplicitFilter>();

        public IReadOnlyDictionary<object, IReadOnlyCollection<object>> AllowedOperations
        { get; set; } = ImmutableDictionary<object, IReadOnlyCollection<object>>.Empty;

        public IReadOnlyDictionary<object, FilterConventionTypeDefinition> TypeDefinitions
        { get; set; } = ImmutableDictionary<object, FilterConventionTypeDefinition>.Empty;

        public IReadOnlyDictionary<object, CreateFieldName> DefaultOperationNames
        { get; set; } = ImmutableDictionary<object, CreateFieldName>.Empty;

        public IReadOnlyDictionary<object, string> DefaultOperationDescriptions
        { get; set; } = ImmutableDictionary<object, string>.Empty;

        public NameString ArgumentName { get; set; }

        public NameString ElementName { get; set; }

        public FilterVisitorDefinitionBase? VisitorDefinition { get; set; }

        public GetFilterTypeName FilterTypeNameFactory { get; set; } =
            FilterConventionExtensions.FilterTypeName;

        public GetFilterTypeDescription FilterTypeDescriptionFactory { get; set; } =
            FilterConventionExtensions.FilterTypeDescription;
    }
}
