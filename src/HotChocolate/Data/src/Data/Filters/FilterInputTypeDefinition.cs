using System;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class FilterInputTypeDefinition
        : InputObjectTypeDefinition
        , IHasScope
    {
        public Type? EntityType { get; set; }

        public string? Scope { get; set; }

        public bool UseOr { get; set; } = true;

        public bool UseAnd { get; set; } = true;
    }
}
