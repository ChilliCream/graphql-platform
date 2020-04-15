using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionTypeDefinition
    {
        public bool Ignore { get; set; }

        public FilterKind FilterKind { get; set; }

        public IReadOnlyCollection<FilterOperationKind> AllowedOperations { get; set; }
            = Array.Empty<FilterOperationKind>();

        public IReadOnlyDictionary<FilterOperationKind, CreateFieldName> OperationNames
        {
            get; set;
        } = ImmutableDictionary<FilterOperationKind, CreateFieldName>.Empty;

        public IReadOnlyDictionary<FilterOperationKind, string> OperationDescriptions
        {
            get; set;
        } = ImmutableDictionary<FilterOperationKind, string>.Empty;
    }
}
