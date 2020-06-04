using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionTypeDefinition
    {
        public bool Ignore { get; set; }

        public int FilterKind { get; set; } = default!;

        public IReadOnlyCollection<int> AllowedOperations { get; set; } =
            Array.Empty<int>();

        public IReadOnlyDictionary<int, CreateFieldName> OperationNames { get; set; } =
            ImmutableDictionary<int, CreateFieldName>.Empty;

        public IReadOnlyDictionary<int, string> OperationDescriptions { get; set; } =
            ImmutableDictionary<int, string>.Empty;
    }
}
