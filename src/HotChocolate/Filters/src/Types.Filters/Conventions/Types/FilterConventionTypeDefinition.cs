using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionTypeDefinition
    {
        public bool Ignore { get; set; }

        public object FilterKind { get; set; } = default!;

        public IReadOnlyCollection<object> AllowedOperations { get; set; } =
            Array.Empty<object>();

        public IReadOnlyDictionary<object, CreateFieldName> OperationNames { get; set; } =
            ImmutableDictionary<object, CreateFieldName>.Empty;

        public IReadOnlyDictionary<object, string> OperationDescriptions { get; set; } =
            ImmutableDictionary<object, string>.Empty;
    }
}
