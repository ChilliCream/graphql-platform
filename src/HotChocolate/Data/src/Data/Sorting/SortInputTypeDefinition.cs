using System;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Sorting
{
    public class SortInputTypeDefinition
        : InputObjectTypeDefinition
        , IHasScope
    {
        public Type? EntityType { get; set; }

        public string? Scope { get; set; }
    }
}
